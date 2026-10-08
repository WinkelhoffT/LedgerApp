using StudyHub.Logic.Domain;
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
        _sut = new StudySessionLifecycle(_timeProvider);
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
        Assert.Equal(new TimeOnly(9, 15), _sut.Create("Graph review", null, null, Date, new TimeOnly(9, 15, 42), 60, null).StartTime);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutTitle_Throws(string? title)
    {
        Assert.Throws<StudySessionValidationException>(() => _sut.Create(title!, null, null, Date, Nine, 60, null));
    }

    [Fact]
    public void Create_WithTooLongTitle_Throws()
    {
        Assert.Throws<StudySessionValidationException>(
            () => _sut.Create(new string('t', StudySession.TitleMaxLength + 1), null, null, Date, Nine, 60, null));
    }

    [Fact]
    public void Create_WithTooLongLocation_Throws()
    {
        Assert.Throws<StudySessionValidationException>(
            () => _sut.Create("Graph review", null, null, Date, Nine, 60, new string('l', StudySession.LocationMaxLength + 1)));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(720)]
    public void Create_WithDurationAtTheLimits_Succeeds(int durationMinutes)
    {
        Assert.Equal(durationMinutes, _sut.Create("Graph review", null, null, Date, Nine, durationMinutes, null).DurationMinutes);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(721)]
    public void Create_WithDurationOutOfRange_Throws(int durationMinutes)
    {
        Assert.Throws<StudySessionValidationException>(() => _sut.Create("Graph review", null, null, Date, Nine, durationMinutes, null));
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
        Assert.Throws<StudySessionValidationException>(() => _sut.Create("Late review", null, null, Date, new TimeOnly(23, 30), 60, null));
    }

    [Fact]
    public void Create_WithCourseAndSemester_Throws()
    {
        Assert.Throws<StudySessionValidationException>(() => _sut.Create("Graph review", CourseId, Guid.NewGuid(), Date, Nine, 60, null));
    }

    [Fact]
    public void Update_ChangesFieldsAndTimestampButKeepsIdentity()
    {
        var session = _sut.Create("Graph review", CourseId, null, Date, Nine, 60, null);
        var semesterId = Guid.NewGuid();
        _timeProvider.UtcNow = Now.AddHours(1);

        var updated = _sut.Update(session, "Exam planning", null, semesterId, Date.AddDays(1), new TimeOnly(14, 30), 45, "Room 101");

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

        Assert.Throws<StudySessionValidationException>(() => _sut.Update(session, "Graph review", null, null, Date, new TimeOnly(23, 30), 60, null));
    }
}
