using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Shared.Analytics;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.StudySessions;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Business.Analytics;

public class StudyTimeOrchestratorTests
{
    // Wednesday, 12:00 in Berlin.
    private static readonly DateTime Now = new(2026, 10, 7, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 10, 7);
    private static readonly DateOnly Monday = new(2026, 10, 5);
    private static readonly DateOnly LastMonday = new(2026, 9, 28);

    private readonly Mock<IStudyAnalyticsRepository> _repository = new();
    private readonly FixedTimeProvider _timeProvider = new(Now);
    private readonly StudyTimeOrchestrator _sut;

    public StudyTimeOrchestratorTests()
    {
        var studyDayProvider = new StudyDayProvider(new FlashcardStudyOptions(), _timeProvider);
        _sut = new StudyTimeOrchestrator(
            _repository.Object,
            studyDayProvider,
            new CalendarPeriodProvider(new CalendarOptions(), _timeProvider),
            new StudyTimeProcessor(new CalendarOptions(), studyDayProvider),
            new StudyStreakProvider()
        );

        Setup([], []);
    }

    private void Setup(
        IReadOnlyList<StudySession> sessions,
        IReadOnlyList<FlashcardReviewActivity> reviews
    )
    {
        _repository
            .Setup(r => r.GetSessionsAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), default))
            .ReturnsAsync(sessions);
        _repository
            .Setup(r =>
                r.GetReviewActivitiesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default)
            )
            .ReturnsAsync(reviews);
        _repository
            .Setup(r =>
                r.GetSubmittedAttemptActivitiesAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    default
                )
            )
            .ReturnsAsync([]);
    }

    private static StudySession Session(DateOnly date, int? actualDurationMinutes) =>
        new(
            Guid.NewGuid(),
            "Graph review",
            null,
            null,
            date,
            new TimeOnly(9, 0),
            60,
            null,
            actualDurationMinutes is null ? null : Now,
            actualDurationMinutes,
            Now,
            Now
        );

    // Berlin is at UTC+2 in September and October 2026.
    private static FlashcardReviewActivity Review(DateOnly date, int minute) =>
        new(date.ToDateTime(new TimeOnly(12, minute), DateTimeKind.Utc), null);

    private void SetupWeeks() =>
        Setup(
            [
                Session(LastMonday, 45),
                Session(LastMonday.AddDays(1), 15),
                Session(LastMonday.AddDays(2), 60),
                Session(LastMonday.AddDays(3), 600),
                Session(Monday, 60),
                Session(Monday.AddDays(1), 120),
                Session(Today, 30),
                Session(Today, null),
                Session(Today.AddDays(1), null),
            ],
            [
                Review(LastMonday.AddDays(1), 0),
                Review(LastMonday.AddDays(1), 1),
                Review(Today, 0),
                Review(Today, 1),
                Review(Today, 2),
            ]
        );

    [Fact]
    public async Task GetStatisticsAsync_ReadsTheLast365StudyDays()
    {
        await _sut.GetStatisticsAsync();

        var fromUtc = new DateTime(2025, 10, 8, 2, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2026, 10, 8, 2, 0, 0, DateTimeKind.Utc);
        _repository.Verify(r =>
            r.GetSessionsAsync(new DateOnly(2025, 10, 8), Today.AddDays(1), default)
        );
        _repository.Verify(r => r.GetReviewActivitiesAsync(fromUtc, toUtc, default));
        _repository.Verify(r => r.GetSubmittedAttemptActivitiesAsync(fromUtc, toUtc, default));
    }

    [Fact]
    public async Task GetStatisticsAsync_ComparesThisWeekWithTheSameDaysOfLastWeek()
    {
        SetupWeeks();

        var statistics = await _sut.GetStatisticsAsync();

        Assert.Equal(Today, statistics.Today);
        Assert.True(statistics.HasStudyTime);
        Assert.Equal(60 + 120 + 30 + 3, statistics.ThisWeekMinutes);
        Assert.Equal(45 + 15 + 2 + 60, statistics.LastWeekToDateMinutes);
        Assert.Equal(3, statistics.ReviewsThisWeek);
        Assert.Equal(2, statistics.ReviewsLastWeekToDate);
    }

    [Fact]
    public async Task GetStatisticsAsync_CountsTheSessionsOfThisWeekUpToToday()
    {
        SetupWeeks();

        var statistics = await _sut.GetStatisticsAsync();

        Assert.Equal(3, statistics.SessionsDoneThisWeek);
        Assert.Equal(4, statistics.SessionsPlannedThisWeek);
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsTheCurrentAndTheLongestStreak()
    {
        SetupWeeks();

        var statistics = await _sut.GetStatisticsAsync();

        Assert.Equal(3, statistics.CurrentStreakDays);
        Assert.Equal(4, statistics.LongestStreakDays);
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsThisWeekFromMondayToSunday()
    {
        SetupWeeks();

        var days = (await _sut.GetStatisticsAsync()).Days;

        Assert.Equal(
            [
                new StudyDayDto(Monday, 60, 0, false, false),
                new StudyDayDto(Monday.AddDays(1), 120, 0, false, false),
                new StudyDayDto(Today, 33, 3, true, false),
                new StudyDayDto(Today.AddDays(1), 0, 0, false, true),
                new StudyDayDto(Today.AddDays(2), 0, 0, false, true),
                new StudyDayDto(Today.AddDays(3), 0, 0, false, true),
                new StudyDayDto(Today.AddDays(4), 0, 0, false, true),
            ],
            days
        );
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsTheLastEightWeeksOldestFirst()
    {
        SetupWeeks();

        var weeks = (await _sut.GetStatisticsAsync()).Weeks;

        Assert.Equal(StudyTimeStatisticsDto.WeekCount, weeks.Count);
        Assert.Equal(new DateOnly(2026, 8, 17), weeks[0].WeekStart);
        Assert.Equal([34, 35, 36, 37, 38, 39, 40, 41], weeks.Select(w => w.IsoWeek));
        Assert.Equal([0, 0, 0, 0, 0, 0, 722, 213], weeks.Select(w => w.Minutes));
    }

    [Fact]
    public async Task GetStatisticsAsync_NumbersTheWeeksAcrossTheYearBoundary()
    {
        _timeProvider.UtcNow = new DateTime(2027, 1, 6, 10, 0, 0, DateTimeKind.Utc);

        var weeks = (await _sut.GetStatisticsAsync()).Weeks;

        Assert.Equal([47, 48, 49, 50, 51, 52, 53, 1], weeks.Select(w => w.IsoWeek));
        Assert.Equal(new DateOnly(2027, 1, 4), weeks[^1].WeekStart);
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsAHeatmapOfFiveWholeWeeks()
    {
        SetupWeeks();

        var heatmap = (await _sut.GetStatisticsAsync()).Heatmap;

        Assert.Equal(35, heatmap.Count);
        Assert.Equal(new DateOnly(2026, 9, 7), heatmap[0].Date);
        Assert.Equal(DayOfWeek.Monday, heatmap[0].Date.DayOfWeek);
        Assert.Equal(new DateOnly(2026, 10, 11), heatmap[^1].Date);
        Assert.Equal(
            new StudyHeatmapDayDto(LastMonday.AddDays(3), 600, 4, false),
            heatmap.Single(d => d.Date == LastMonday.AddDays(3))
        );
        Assert.Equal(
            new StudyHeatmapDayDto(Today, 33, 2, false),
            heatmap.Single(d => d.Date == Today)
        );
        Assert.All(heatmap.Where(d => d.Date > Today), d => Assert.True(d.IsFuture));
    }

    [Fact]
    public async Task GetStatisticsAsync_WithoutStudyTime_ReturnsEmptyStatistics()
    {
        Setup([Session(Today, null)], []);

        var statistics = await _sut.GetStatisticsAsync();

        Assert.False(statistics.HasStudyTime);
        Assert.Equal(0, statistics.ThisWeekMinutes);
        Assert.Equal(0, statistics.CurrentStreakDays);
        Assert.Equal(0, statistics.LongestStreakDays);
        Assert.Equal(0, statistics.SessionsDoneThisWeek);
        Assert.Equal(1, statistics.SessionsPlannedThisWeek);
        Assert.All(statistics.Heatmap, d => Assert.Equal(0, d.Level));
    }
}
