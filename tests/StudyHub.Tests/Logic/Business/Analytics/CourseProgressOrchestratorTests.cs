using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Shared.Analytics;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Business.Analytics;

public class CourseProgressOrchestratorTests
{
    // 12:00 in Berlin.
    private static readonly DateTime Now = new(2026, 10, 7, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 10, 7);
    private static readonly DateOnly SemesterStart = new(2026, 10, 1);

    private readonly Mock<IStudyAnalyticsRepository> _analyticsRepository = new();
    private readonly Mock<ISemesterRepository> _semesterRepository = new();
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly CourseProgressOrchestrator _sut;
    private readonly Semester _semester = new(
        Guid.NewGuid(),
        "Winter 2026/27",
        SemesterStart,
        new DateOnly(2027, 3, 31),
        false,
        Now,
        Now
    );

    public CourseProgressOrchestratorTests()
    {
        var studyDayProvider = new StudyDayProvider(
            new FlashcardStudyOptions(),
            new FixedTimeProvider(Now)
        );
        _sut = new CourseProgressOrchestrator(
            _analyticsRepository.Object,
            _semesterRepository.Object,
            _courseRepository.Object,
            new ActiveSemesterProvider(),
            studyDayProvider,
            new StudyTimeProcessor(new CalendarOptions(), studyDayProvider),
            new CourseProgressProcessor()
        );

        _semesterRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([_semester]);
        _courseRepository
            .Setup(r => r.GetBySemesterIdAsync(_semester.Id, default))
            .ReturnsAsync([]);
        _analyticsRepository.Setup(r => r.GetDeckProgressCountsAsync(default)).ReturnsAsync([]);
        _analyticsRepository.Setup(r => r.GetGradedResultsAsync(default)).ReturnsAsync([]);
        _analyticsRepository
            .Setup(r => r.GetSessionsAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), default))
            .ReturnsAsync([]);
        _analyticsRepository
            .Setup(r =>
                r.GetReviewActivitiesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default)
            )
            .ReturnsAsync([]);
        _analyticsRepository
            .Setup(r =>
                r.GetSubmittedAttemptActivitiesAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    default
                )
            )
            .ReturnsAsync([]);
    }

    private Course Course(string name, bool isArchived = false) =>
        new(Guid.NewGuid(), name, null, "#2563eb", _semester.Id, isArchived, Now, Now);

    private void SetupCourses(params Course[] courses) =>
        _courseRepository
            .Setup(r => r.GetBySemesterIdAsync(_semester.Id, default))
            .ReturnsAsync(courses);

    [Fact]
    public async Task GetOverviewAsync_WithoutActiveSemester_ReturnsTheEmptyState()
    {
        _semesterRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);

        var overview = await _sut.GetOverviewAsync();

        Assert.Same(CourseProgressOverviewDto.Empty, overview);
        _analyticsRepository.Verify(r => r.GetDeckProgressCountsAsync(default), Times.Never);
    }

    [Fact]
    public async Task GetOverviewAsync_ListsTheCoursesThatAreNotArchivedByName()
    {
        var databases = Course("databases");
        var algorithms = Course("Algorithms");
        SetupCourses(databases, Course("Archived", isArchived: true), algorithms);

        var overview = await _sut.GetOverviewAsync();

        Assert.True(overview.HasActiveSemester);
        Assert.Equal("Winter 2026/27", overview.SemesterName);
        Assert.Equal([algorithms.Id, databases.Id], overview.Courses.Select(c => c.CourseId));
    }

    [Fact]
    public async Task GetOverviewAsync_CombinesTheDecksAndExamResultsOfEachCourse()
    {
        var algorithms = Course("Algorithms");
        var databases = Course("Databases");
        SetupCourses(algorithms, databases);
        _analyticsRepository
            .Setup(r => r.GetDeckProgressCountsAsync(default))
            .ReturnsAsync([
                new FlashcardDeckProgressCounts(Guid.NewGuid(), algorithms.Id, 5, 1, 2, 2),
                new FlashcardDeckProgressCounts(Guid.NewGuid(), algorithms.Id, 0, 0, 3, 7),
                new FlashcardDeckProgressCounts(Guid.NewGuid(), null, 0, 0, 0, 50),
            ]);
        _analyticsRepository
            .Setup(r => r.GetGradedResultsAsync(default))
            .ReturnsAsync([
                new PracticeExamResult(algorithms.Id, Now.AddDays(-2), 36, 40),
                new PracticeExamResult(algorithms.Id, Now.AddDays(-1), 20, 40),
                new PracticeExamResult(null, Now, 40, 40),
            ]);

        var overview = await _sut.GetOverviewAsync();

        var algorithmsProgress = overview.Courses[0];
        Assert.Equal(new FlashcardProgressDto(5, 1, 5, 9, 20, 70), algorithmsProgress.Flashcards);
        Assert.Equal(50, algorithmsProgress.LatestExamPercent);
        Assert.Equal(90, algorithmsProgress.BestExamPercent);
        Assert.Equal(2, algorithmsProgress.GradedAttempts);

        var databasesProgress = overview.Courses[1];
        Assert.Equal(new FlashcardProgressDto(0, 0, 0, 0, 0, null), databasesProgress.Flashcards);
        Assert.Null(databasesProgress.LatestExamPercent);
        Assert.Equal(0, databasesProgress.GradedAttempts);
    }

    [Fact]
    public async Task GetOverviewAsync_CountsStudyTimeSinceTheSemesterStart()
    {
        var algorithms = Course("Algorithms");
        SetupCourses(algorithms);
        var fromUtc = new DateTime(2026, 10, 1, 2, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2026, 10, 8, 2, 0, 0, DateTimeKind.Utc);
        _analyticsRepository
            .Setup(r => r.GetSessionsAsync(SemesterStart, Today.AddDays(1), default))
            .ReturnsAsync([
                new StudySession(
                    Guid.NewGuid(),
                    "Graph review",
                    algorithms.Id,
                    null,
                    new DateOnly(2026, 10, 2),
                    new TimeOnly(9, 0),
                    60,
                    null,
                    Now,
                    75,
                    Now,
                    Now
                ),
            ]);
        _analyticsRepository
            .Setup(r => r.GetReviewActivitiesAsync(fromUtc, toUtc, default))
            .ReturnsAsync([new FlashcardReviewActivity(Now, algorithms.Id)]);

        var overview = await _sut.GetOverviewAsync();

        Assert.Equal(76, Assert.Single(overview.Courses).StudyMinutes);
    }
}
