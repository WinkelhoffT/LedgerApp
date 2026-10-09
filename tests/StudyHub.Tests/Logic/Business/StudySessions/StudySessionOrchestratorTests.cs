using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Business.StudySessions;

public class StudySessionOrchestratorTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Date = new(2026, 10, 8);
    private static readonly TimeOnly Nine = new(9, 0);

    private readonly Mock<IStudySessionRepository> _sessionRepository = new();
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly Mock<ISemesterRepository> _semesterRepository = new();
    private readonly StudySessionOrchestrator _sut;

    public StudySessionOrchestratorTests()
    {
        _sut = new StudySessionOrchestrator(
            _sessionRepository.Object,
            new StudySessionLifecycle(new FixedTimeProvider(Now)),
            _courseRepository.Object,
            _semesterRepository.Object
        );
    }

    private StudySession SetupSession(Guid? courseId = null, Guid? semesterId = null)
    {
        var session = new StudySession(
            Guid.NewGuid(),
            "Graph review",
            courseId,
            semesterId,
            Date,
            Nine,
            60,
            null,
            null,
            null,
            Now,
            Now
        );
        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id, default)).ReturnsAsync(session);
        return session;
    }

    private Course SetupCourse(bool isArchived = false)
    {
        var course = new Course(
            Guid.NewGuid(),
            "Algorithms",
            null,
            "#2563eb",
            Guid.NewGuid(),
            isArchived,
            Now,
            Now
        );
        _courseRepository.Setup(r => r.GetByIdAsync(course.Id, default)).ReturnsAsync(course);
        return course;
    }

    private Semester SetupSemester(bool isArchived = false)
    {
        var semester = new Semester(
            Guid.NewGuid(),
            "Winter 2026/27",
            new DateOnly(2026, 10, 1),
            new DateOnly(2027, 3, 31),
            isArchived,
            Now,
            Now
        );
        _semesterRepository.Setup(r => r.GetByIdAsync(semester.Id, default)).ReturnsAsync(semester);
        return semester;
    }

    private static CreateStudySessionRequest CreateRequest(
        Guid? courseId = null,
        Guid? semesterId = null,
        int durationMinutes = 90
    ) => new("Graph review", courseId, semesterId, Date, Nine, durationMinutes, "Library");

    private static UpdateStudySessionRequest UpdateRequest(
        Guid id,
        Guid? courseId = null,
        Guid? semesterId = null
    ) => new(id, "Exam planning", courseId, semesterId, Date, new TimeOnly(14, 0), 45, null);

    [Fact]
    public async Task CreateAsync_WithCourse_SavesSessionAndReturnsCourseNameAndColor()
    {
        var course = SetupCourse();

        var result = await _sut.CreateAsync(CreateRequest(course.Id));

        Assert.Equal("Algorithms", result.OwnerName);
        Assert.Equal("#2563eb", result.Color);
        Assert.Equal(new TimeOnly(10, 30), result.EndTime);
        Assert.Equal("Library", result.Location);
        _sessionRepository.Verify(
            r =>
                r.AddAsync(
                    It.Is<StudySession>(s => s.Title == "Graph review" && s.CourseId == course.Id),
                    default
                ),
            Times.Once
        );
        _sessionRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithSemester_ReturnsSemesterNameWithoutColor()
    {
        var semester = SetupSemester();

        var result = await _sut.CreateAsync(CreateRequest(semesterId: semester.Id));

        Assert.Equal("Winter 2026/27", result.OwnerName);
        Assert.Null(result.Color);
    }

    [Fact]
    public async Task CreateAsync_WithoutOwner_ReturnsNoOwnerName()
    {
        var result = await _sut.CreateAsync(CreateRequest());

        Assert.Null(result.OwnerName);
        Assert.Null(result.Color);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidDuration_ThrowsWithoutSaving()
    {
        await Assert.ThrowsAsync<StudySessionValidationException>(() =>
            _sut.CreateAsync(CreateRequest(durationMinutes: 721))
        );
        _sessionRepository.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownCourse_Throws()
    {
        await Assert.ThrowsAsync<CourseNotFoundException>(() =>
            _sut.CreateAsync(CreateRequest(Guid.NewGuid()))
        );
        _sessionRepository.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithArchivedCourse_Throws()
    {
        var course = SetupCourse(isArchived: true);

        await Assert.ThrowsAsync<CourseArchivedException>(() =>
            _sut.CreateAsync(CreateRequest(course.Id))
        );
        _sessionRepository.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownSemester_Throws()
    {
        await Assert.ThrowsAsync<SemesterNotFoundException>(() =>
            _sut.CreateAsync(CreateRequest(semesterId: Guid.NewGuid()))
        );
    }

    [Fact]
    public async Task CreateAsync_WithArchivedSemester_Throws()
    {
        var semester = SetupSemester(isArchived: true);

        await Assert.ThrowsAsync<SemesterArchivedException>(() =>
            _sut.CreateAsync(CreateRequest(semesterId: semester.Id))
        );
    }

    [Fact]
    public async Task UpdateAsync_ChangesTheSession()
    {
        var session = SetupSession();

        var result = await _sut.UpdateAsync(UpdateRequest(session.Id));

        Assert.Equal(session.Id, result.Id);
        Assert.Equal("Exam planning", result.Title);
        Assert.Equal(new TimeOnly(14, 45), result.EndTime);
        _sessionRepository.Verify(
            r => r.Update(It.Is<StudySession>(s => s.Id == session.Id && s.DurationMinutes == 45)),
            Times.Once
        );
        _sessionRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_KeepsAnAlreadyLinkedArchivedCourse()
    {
        var course = SetupCourse(isArchived: true);
        var session = SetupSession(course.Id);

        var result = await _sut.UpdateAsync(UpdateRequest(session.Id, course.Id));

        Assert.Equal(course.Id, result.CourseId);
        Assert.Equal("Algorithms", result.OwnerName);
    }

    [Fact]
    public async Task UpdateAsync_SwitchingToAnArchivedCourse_Throws()
    {
        var course = SetupCourse(isArchived: true);
        var session = SetupSession();

        await Assert.ThrowsAsync<CourseArchivedException>(() =>
            _sut.UpdateAsync(UpdateRequest(session.Id, course.Id))
        );
        _sessionRepository.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_KeepsAnAlreadyLinkedArchivedSemester()
    {
        var semester = SetupSemester(isArchived: true);
        var session = SetupSession(semesterId: semester.Id);

        var result = await _sut.UpdateAsync(UpdateRequest(session.Id, semesterId: semester.Id));

        Assert.Equal(semester.Id, result.SemesterId);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownSession_Throws()
    {
        await Assert.ThrowsAsync<StudySessionNotFoundException>(() =>
            _sut.UpdateAsync(UpdateRequest(Guid.NewGuid()))
        );
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheSession()
    {
        var session = SetupSession();

        await _sut.DeleteAsync(session.Id);

        _sessionRepository.Verify(r => r.Remove(session), Times.Once);
        _sessionRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownSession_Throws()
    {
        var id = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<StudySessionNotFoundException>(() =>
            _sut.DeleteAsync(id)
        );

        Assert.Equal(id, ex.StudySessionId);
        _sessionRepository.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }
}
