using StudyHub.Logic.Domain;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.StudySessions;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Domain.StudySessions;

public class StudySessionLifecycleTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Date = new(2026, 10, 8);
    private static readonly TimeOnly Nine = new(9, 0);
    private static readonly Guid CourseId = Guid.NewGuid();

    private readonly FixedTimeProvider _timeProvider = new(Now);
    private readonly StudySessionLifecycle _sut;

    public StudySessionLifecycleTests()
    {
        _sut = new StudySessionLifecycle(
            _timeProvider,
            new CalendarPeriodProvider(new CalendarOptions(), _timeProvider)
        );
    }

    [Fact]
    public void Create_ReturnsSessionWithTrimmedTitleAndLocation()
    {
        var session = _sut.Create("  Graph review ", CourseId, null, Date, Nine, 90, " Library ");

        Assert.Equal("Graph review", session.Title);
        Assert.Equal("Library", session.Location);
        Assert.Equal(CourseId, session.CourseId);
        Assert.Equal(Date, session.Date);
        Assert.Equal(Nine, session.StartTime);
        Assert.Equal(90, session.DurationMinutes);
        Assert.Equal(7, session.Id.Version);
        Assert.Equal(Now, session.CreatedAt);
        Assert.Equal(Now, session.UpdatedAt);
        Assert.Null(session.CompletedAt);
        Assert.Null(session.ActualDurationMinutes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyLocation_StoresNoLocation(string location)
    {
        Assert.Null(_sut.Create("Graph review", null, null, Date, Nine, 60, location).Location);
    }

    [Fact]
    public void Create_DropsSecondsOfTheStartTime()
    {
        Assert.Equal(
            new TimeOnly(9, 15),
            _sut.Create(
                "Graph review",
                null,
                null,
                Date,
                new TimeOnly(9, 15, 42),
                60,
                null
            ).StartTime
        );
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutTitle_Throws(string? title)
    {
        Assert.Throws<StudySessionValidationException>(() =>
            _sut.Create(title!, null, null, Date, Nine, 60, null)
        );
    }

    [Fact]
    public void Create_WithTooLongTitle_Throws()
    {
        Assert.Throws<StudySessionValidationException>(() =>
            _sut.Create(
                new string('t', StudySession.TitleMaxLength + 1),
                null,
                null,
                Date,
                Nine,
                60,
                null
            )
        );
    }

    [Fact]
    public void Create_WithTooLongLocation_Throws()
    {
        Assert.Throws<StudySessionValidationException>(() =>
            _sut.Create(
                "Graph review",
                null,
                null,
                Date,
                Nine,
                60,
                new string('l', StudySession.LocationMaxLength + 1)
            )
        );
    }

    [Theory]
    [InlineData(5)]
    [InlineData(720)]
    public void Create_WithDurationAtTheLimits_Succeeds(int durationMinutes)
    {
        Assert.Equal(
            durationMinutes,
            _sut.Create(
                "Graph review",
                null,
                null,
                Date,
                Nine,
                durationMinutes,
                null
            ).DurationMinutes
        );
    }

    [Theory]
    [InlineData(4)]
    [InlineData(721)]
    public void Create_WithDurationOutOfRange_Throws(int durationMinutes)
    {
        Assert.Throws<StudySessionValidationException>(() =>
            _sut.Create("Graph review", null, null, Date, Nine, durationMinutes, null)
        );
    }

    [Fact]
    public void Create_EndingAtMidnight_Succeeds()
    {
        var session = _sut.Create("Late review", null, null, Date, new TimeOnly(23, 0), 60, null);

        Assert.Equal(new TimeOnly(23, 0), session.StartTime);
    }

    [Fact]
    public void Create_EndingAfterMidnight_Throws()
    {
        Assert.Throws<StudySessionValidationException>(() =>
            _sut.Create("Late review", null, null, Date, new TimeOnly(23, 30), 60, null)
        );
    }

    [Fact]
    public void Create_WithCourseAndSemester_Throws()
    {
        Assert.Throws<StudySessionValidationException>(() =>
            _sut.Create("Graph review", CourseId, Guid.NewGuid(), Date, Nine, 60, null)
        );
    }

    [Fact]
    public void Update_ChangesFieldsAndTimestampButKeepsIdentity()
    {
        var session = _sut.Create("Graph review", CourseId, null, Date, Nine, 60, null);
        var semesterId = Guid.NewGuid();
        _timeProvider.UtcNow = Now.AddHours(1);

        var updated = _sut.Update(
            session,
            "Exam planning",
            null,
            semesterId,
            Date.AddDays(1),
            new TimeOnly(14, 30),
            45,
            "Room 101"
        );

        Assert.Equal(session.Id, updated.Id);
        Assert.Equal("Exam planning", updated.Title);
        Assert.Null(updated.CourseId);
        Assert.Equal(semesterId, updated.SemesterId);
        Assert.Equal(Date.AddDays(1), updated.Date);
        Assert.Equal(new TimeOnly(14, 30), updated.StartTime);
        Assert.Equal(45, updated.DurationMinutes);
        Assert.Equal("Room 101", updated.Location);
        Assert.Equal(Now, updated.CreatedAt);
        Assert.Equal(Now.AddHours(1), updated.UpdatedAt);
    }

    [Fact]
    public void Update_EndingAfterMidnight_Throws()
    {
        var session = _sut.Create("Graph review", null, null, Date, Nine, 60, null);

        Assert.Throws<StudySessionValidationException>(() =>
            _sut.Update(session, "Graph review", null, null, Date, new TimeOnly(23, 30), 60, null)
        );
    }

    [Fact]
    public void Complete_StoresTheActualDurationAndWhenTheSessionWasDone()
    {
        var session = _sut.Create("Graph review", CourseId, null, Date, Nine, 60, null);
        _timeProvider.UtcNow = Now.AddHours(1);

        var completed = _sut.Complete(session, 75);

        Assert.Equal(Now.AddHours(1), completed.CompletedAt);
        Assert.Equal(75, completed.ActualDurationMinutes);
        Assert.Equal(60, completed.DurationMinutes);
        Assert.Equal(Now.AddHours(1), completed.UpdatedAt);
    }

    [Fact]
    public void Complete_APastSession_Succeeds()
    {
        var session = _sut.Create("Graph review", null, null, Date.AddDays(-3), Nine, 60, null);

        Assert.Equal(60, _sut.Complete(session, 60).ActualDurationMinutes);
    }

    [Fact]
    public void Complete_AFutureSession_Throws()
    {
        var session = _sut.Create("Graph review", null, null, Date.AddDays(1), Nine, 60, null);

        Assert.Throws<StudySessionValidationException>(() => _sut.Complete(session, 60));
    }

    [Fact]
    public void Complete_UsesTheCalendarTimeZoneForToday()
    {
        // 22:30 UTC is already 00:30 of the next day in Berlin.
        _timeProvider.UtcNow = new DateTime(2026, 10, 8, 22, 30, 0, DateTimeKind.Utc);
        var session = _sut.Create("Night review", null, null, Date.AddDays(1), Nine, 60, null);

        Assert.NotNull(_sut.Complete(session, 60).CompletedAt);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(720)]
    public void Complete_WithActualDurationAtTheLimits_Succeeds(int actualDurationMinutes)
    {
        var session = _sut.Create("Graph review", null, null, Date, TimeOnly.MinValue, 60, null);

        Assert.Equal(
            actualDurationMinutes,
            _sut.Complete(session, actualDurationMinutes).ActualDurationMinutes
        );
    }

    [Theory]
    [InlineData(4)]
    [InlineData(721)]
    public void Complete_WithActualDurationOutOfRange_Throws(int actualDurationMinutes)
    {
        var session = _sut.Create("Graph review", null, null, Date, TimeOnly.MinValue, 60, null);

        Assert.Throws<StudySessionValidationException>(() =>
            _sut.Complete(session, actualDurationMinutes)
        );
    }

    [Fact]
    public void Complete_EndingAtMidnight_Succeeds()
    {
        var session = _sut.Create("Late review", null, null, Date, new TimeOnly(23, 0), 30, null);

        Assert.Equal(60, _sut.Complete(session, 60).ActualDurationMinutes);
    }

    [Fact]
    public void Complete_EndingAfterMidnight_Throws()
    {
        var session = _sut.Create("Late review", null, null, Date, new TimeOnly(23, 30), 30, null);

        Assert.Throws<StudySessionValidationException>(() => _sut.Complete(session, 60));
    }

    [Fact]
    public void Complete_ASessionThatIsDone_ChangesTheDurationButKeepsWhenItWasDone()
    {
        var session = _sut.Complete(
            _sut.Create("Graph review", null, null, Date, Nine, 60, null),
            60
        );
        _timeProvider.UtcNow = Now.AddHours(2);

        var changed = _sut.Complete(session, 90);

        Assert.Equal(Now, changed.CompletedAt);
        Assert.Equal(90, changed.ActualDurationMinutes);
        Assert.Equal(Now.AddHours(2), changed.UpdatedAt);
    }

    [Fact]
    public void ResetCompletion_MakesTheSessionPlannedOnlyAgain()
    {
        var session = _sut.Complete(
            _sut.Create("Graph review", null, null, Date, Nine, 60, null),
            60
        );
        _timeProvider.UtcNow = Now.AddHours(1);

        var reset = _sut.ResetCompletion(session);

        Assert.Null(reset.CompletedAt);
        Assert.Null(reset.ActualDurationMinutes);
        Assert.Equal(Now.AddHours(1), reset.UpdatedAt);
    }

    [Fact]
    public void Update_ASessionThatIsDone_KeepsTheCompletion()
    {
        var session = _sut.Complete(
            _sut.Create("Graph review", null, null, Date, Nine, 60, null),
            75
        );

        var updated = _sut.Update(
            session,
            "Exam planning",
            null,
            null,
            Date.AddDays(-1),
            new TimeOnly(14, 0),
            45,
            null
        );

        Assert.Equal(Now, updated.CompletedAt);
        Assert.Equal(75, updated.ActualDurationMinutes);
    }

    [Fact]
    public void Update_MovingASessionThatIsDoneToTheFuture_Throws()
    {
        var session = _sut.Complete(
            _sut.Create("Graph review", null, null, Date, Nine, 60, null),
            60
        );

        Assert.Throws<StudySessionValidationException>(() =>
            _sut.Update(session, "Graph review", null, null, Date.AddDays(1), Nine, 60, null)
        );
    }

    [Fact]
    public void Update_MovingASessionThatIsDoneSoItsActualDurationEndsAfterMidnight_Throws()
    {
        var session = _sut.Complete(
            _sut.Create("Graph review", null, null, Date, Nine, 30, null),
            120
        );

        Assert.Throws<StudySessionValidationException>(() =>
            _sut.Update(session, "Graph review", null, null, Date, new TimeOnly(23, 0), 30, null)
        );
    }
}
