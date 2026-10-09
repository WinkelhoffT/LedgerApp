using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Analytics;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.StudySessions;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Domain.Analytics;

public class StudyTimeProcessorTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Wednesday = new(2026, 10, 7);
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid OtherCourseId = Guid.NewGuid();

    private readonly StudyTimeProcessor _sut = new(
        new CalendarOptions(),
        new StudyDayProvider(new FlashcardStudyOptions(), new FixedTimeProvider(Now))
    );

    private static DateTime Utc(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second = 0
    ) => new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    private static StudySession Session(
        DateOnly date,
        TimeOnly startTime,
        int? actualDurationMinutes,
        Guid? courseId = null,
        int plannedMinutes = 60
    ) =>
        new(
            Guid.NewGuid(),
            "Graph review",
            courseId,
            null,
            date,
            startTime,
            plannedMinutes,
            null,
            actualDurationMinutes is null ? null : Now,
            actualDurationMinutes,
            Now,
            Now
        );

    private static StudyActivities Activities(
        IReadOnlyList<StudySession>? sessions = null,
        IReadOnlyList<FlashcardReviewActivity>? reviews = null,
        IReadOnlyList<PracticeExamAttemptActivity>? attempts = null
    ) => new(sessions ?? [], reviews ?? [], attempts ?? []);

    private static List<FlashcardReviewActivity> Reviews(
        DateTime first,
        int count,
        int gapSeconds,
        Guid? courseId = null
    ) =>
        Enumerable
            .Range(0, count)
            .Select(i => new FlashcardReviewActivity(first.AddSeconds(i * gapSeconds), courseId))
            .ToList();

    [Fact]
    public void GetDays_WithoutActivities_ReturnsNoDays()
    {
        Assert.Empty(_sut.GetDays(Activities()));
    }

    [Fact]
    public void GetDays_ASingleAnswer_CountsOneMinute()
    {
        var day = Assert.Single(
            _sut.GetDays(Activities(reviews: Reviews(Utc(2026, 10, 7, 8, 0), 1, 0)))
        );

        Assert.Equal(Wednesday, day.Date);
        Assert.Equal(1, day.Minutes);
        Assert.Equal(1, day.ReviewCount);
    }

    // 60 answers: the first counts 60 s, every further one the gap to the previous answer, at most 60 s.
    [Theory]
    [InlineData(10, 11)]
    [InlineData(60, 60)]
    [InlineData(61, 60)]
    [InlineData(600, 60)]
    public void GetDays_CountsTheGapBetweenAnswersUpToOneMinute(int gapSeconds, int expectedMinutes)
    {
        var day = Assert.Single(
            _sut.GetDays(Activities(reviews: Reviews(Utc(2026, 10, 7, 8, 0), 60, gapSeconds)))
        );

        Assert.Equal(expectedMinutes, day.Minutes);
        Assert.Equal(60, day.ReviewCount);
    }

    [Fact]
    public void GetDays_ACompletedSession_CountsItsActualDuration()
    {
        var day = Assert.Single(
            _sut.GetDays(
                Activities(sessions: [Session(Wednesday, new TimeOnly(9, 0), 75, CourseId, 120)])
            )
        );

        Assert.Equal(75, day.Minutes);
        Assert.Equal(0, day.ReviewCount);
        Assert.Equal(75, day.MinutesByCourse[CourseId]);
    }

    [Fact]
    public void GetDays_ASessionThatIsNotDone_CountsNothing()
    {
        Assert.Empty(
            _sut.GetDays(Activities(sessions: [Session(Wednesday, new TimeOnly(9, 0), null)]))
        );
    }

    [Theory]
    [InlineData(45, 45)]
    [InlineData(180, 120)]
    public void GetDays_ASubmittedAttempt_CountsUpToTwiceTheExamDuration(
        int minutesToSubmission,
        int expectedMinutes
    )
    {
        var start = Utc(2026, 10, 7, 8, 0);

        var day = Assert.Single(
            _sut.GetDays(
                Activities(
                    attempts:
                    [
                        new PracticeExamAttemptActivity(
                            start,
                            start.AddMinutes(minutesToSubmission),
                            60,
                            CourseId
                        ),
                    ]
                )
            )
        );

        Assert.Equal(expectedMinutes, day.Minutes);
        Assert.Equal(expectedMinutes, day.MinutesByCourse[CourseId]);
    }

    // The session runs from 09:00 to 10:30 Berlin time, which is 07:00 to 08:30 UTC.
    [Fact]
    public void GetDays_AnswersDuringACompletedSession_AreNotCountedTwice()
    {
        var session = Session(Wednesday, new TimeOnly(9, 0), 90);
        var reviews = Reviews(Utc(2026, 10, 7, 7, 10), 100, 20, CourseId);

        var day = Assert.Single(_sut.GetDays(Activities([session], reviews)));

        Assert.Equal(90, day.Minutes);
        Assert.Equal(100, day.ReviewCount);
        Assert.Empty(day.MinutesByCourse);
    }

    [Fact]
    public void GetDays_AnAnswerThatStartsDuringTheSession_AddsOnlyThePartAfterIt()
    {
        var session = Session(Wednesday, new TimeOnly(9, 0), 90);
        FlashcardReviewActivity[] reviews =
        [
            new(Utc(2026, 10, 7, 8, 29, 30), CourseId),
            new(Utc(2026, 10, 7, 8, 30, 30), CourseId),
            new(Utc(2026, 10, 7, 8, 31, 0), CourseId),
        ];

        var day = Assert.Single(_sut.GetDays(Activities([session], reviews)));

        Assert.Equal(91, day.Minutes);
        Assert.Equal(1, day.MinutesByCourse[CourseId]);
    }

    [Fact]
    public void GetDays_AnActivityStartingWithASession_GoesToTheSession()
    {
        var session = Session(Wednesday, new TimeOnly(9, 0), 60, CourseId);
        var attempt = new PracticeExamAttemptActivity(
            Utc(2026, 10, 7, 7, 0),
            Utc(2026, 10, 7, 7, 30),
            60,
            OtherCourseId
        );

        var day = Assert.Single(_sut.GetDays(Activities([session], attempts: [attempt])));

        Assert.Equal(60, day.Minutes);
        Assert.Equal(60, day.MinutesByCourse[CourseId]);
        Assert.False(day.MinutesByCourse.ContainsKey(OtherCourseId));
    }

    [Fact]
    public void GetDays_OverlappingTime_GoesToTheActivityThatStartedFirst()
    {
        PracticeExamAttemptActivity[] attempts =
        [
            new(Utc(2026, 10, 7, 7, 0), Utc(2026, 10, 7, 8, 0), 60, CourseId),
            new(Utc(2026, 10, 7, 7, 30), Utc(2026, 10, 7, 8, 30), 60, OtherCourseId),
        ];

        var day = Assert.Single(_sut.GetDays(Activities(attempts: attempts)));

        Assert.Equal(90, day.Minutes);
        Assert.Equal(60, day.MinutesByCourse[CourseId]);
        Assert.Equal(30, day.MinutesByCourse[OtherCourseId]);
    }

    // The study day starts at 04:00 Berlin time, which is 02:00 UTC in October.
    [Fact]
    public void GetDays_SplitsTimeAtTheStartOfTheStudyDay()
    {
        var days = _sut.GetDays(Activities(reviews: Reviews(Utc(2026, 10, 8, 1, 51), 20, 60)));

        Assert.Equal(2, days.Count);
        Assert.Equal((Wednesday, 10, 9), (days[0].Date, days[0].Minutes, days[0].ReviewCount));
        Assert.Equal(
            (Wednesday.AddDays(1), 10, 11),
            (days[1].Date, days[1].Minutes, days[1].ReviewCount)
        );
    }

    [Fact]
    public void GetDays_StudyingAfterMidnight_CountsForTheEveningBefore()
    {
        var days = _sut.GetDays(
            Activities(sessions: [Session(Wednesday.AddDays(1), new TimeOnly(0, 30), 60)])
        );

        Assert.Equal((Wednesday, 60), (Assert.Single(days).Date, days[0].Minutes));
    }

    // On 25 Oct 2026 Berlin falls back to UTC+1: 03:30 local is 02:30 UTC, and the study day starts at 03:00 UTC.
    [Fact]
    public void GetDays_OnTheAutumnClockChange_ConvertsTheSessionWithWinterTime()
    {
        var days = _sut.GetDays(
            Activities(sessions: [Session(new DateOnly(2026, 10, 25), new TimeOnly(3, 30), 60)])
        );

        Assert.Equal(
            [(new DateOnly(2026, 10, 24), 30), (new DateOnly(2026, 10, 25), 30)],
            days.Select(d => (d.Date, d.Minutes))
        );
    }

    // On 28 Mar 2027 Berlin springs forward to UTC+2: 03:00 local is 01:00 UTC, and the study day starts at 02:00 UTC.
    [Fact]
    public void GetDays_OnTheSpringClockChange_ConvertsTheSessionWithSummerTime()
    {
        var days = _sut.GetDays(
            Activities(sessions: [Session(new DateOnly(2027, 3, 28), new TimeOnly(3, 0), 120)])
        );

        Assert.Equal(
            [(new DateOnly(2027, 3, 27), 60), (new DateOnly(2027, 3, 28), 60)],
            days.Select(d => (d.Date, d.Minutes))
        );
    }

    // 02:30 does not exist on 28 Mar 2027; the session starts at 03:30 summer time (01:30 UTC) instead.
    [Fact]
    public void GetDays_ASessionStartingInTheSpringGap_MovesForward()
    {
        var days = _sut.GetDays(
            Activities(sessions: [Session(new DateOnly(2027, 3, 28), new TimeOnly(2, 30), 60)])
        );

        Assert.Equal(
            [(new DateOnly(2027, 3, 27), 30), (new DateOnly(2027, 3, 28), 30)],
            days.Select(d => (d.Date, d.Minutes))
        );
    }

    [Fact]
    public void GetDays_TimeWithoutACourse_CountsOnlyForTheDay()
    {
        var semesterSession = Session(Wednesday, new TimeOnly(9, 0), 30) with
        {
            SemesterId = Guid.NewGuid(),
        };
        var deckWithoutCourse = Reviews(Utc(2026, 10, 7, 10, 0), 1, 0);
        var examFromCourseDeck = new PracticeExamAttemptActivity(
            Utc(2026, 10, 7, 12, 0),
            Utc(2026, 10, 7, 12, 45),
            60,
            CourseId
        );

        var day = Assert.Single(
            _sut.GetDays(Activities([semesterSession], deckWithoutCourse, [examFromCourseDeck]))
        );

        Assert.Equal(76, day.Minutes);
        Assert.Equal(45, Assert.Single(day.MinutesByCourse).Value);
    }

    [Fact]
    public void GetDays_RoundsTheDayNotEachInterval()
    {
        // 30 single answers 2 minutes apart count 60 s each; 3 answers 20 s apart count 60 + 20 + 20 s.
        var reviews = Reviews(Utc(2026, 10, 7, 8, 0), 30, 120)
            .Concat(Reviews(Utc(2026, 10, 7, 12, 0), 3, 20))
            .ToList();

        Assert.Equal(32, Assert.Single(_sut.GetDays(Activities(reviews: reviews))).Minutes);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(29, 1)]
    [InlineData(30, 2)]
    [InlineData(59, 2)]
    [InlineData(60, 3)]
    [InlineData(119, 3)]
    [InlineData(120, 4)]
    [InlineData(600, 4)]
    public void GetHeatLevel_UsesFixedBoundaries(int minutes, int expectedLevel)
    {
        Assert.Equal(expectedLevel, _sut.GetHeatLevel(minutes));
    }
}
