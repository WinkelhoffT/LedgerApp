using StudyHub.Logic.Domain;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Domain.CalendarEvents;

public class CalendarEventLifecycleTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Date = new(2026, 11, 20);
    private static readonly TimeOnly Ten = new(10, 0);
    private static readonly Guid CourseId = Guid.NewGuid();

    private readonly FixedTimeProvider _timeProvider = new(Now);
    private readonly CalendarEventLifecycle _sut;

    public CalendarEventLifecycleTests()
    {
        _sut = new CalendarEventLifecycle(_timeProvider);
    }

    private CalendarEvent Exam(TimeOnly? startTime, int? durationMinutes) =>
        _sut.Create(
            CalendarEventKind.Exam,
            "Algorithms exam",
            CourseId,
            null,
            Date,
            startTime,
            durationMinutes,
            null
        );

    private CalendarEvent Deadline(TimeOnly? startTime, int? durationMinutes = null) =>
        _sut.Create(
            CalendarEventKind.Deadline,
            "Exercise sheet 3",
            CourseId,
            null,
            Date,
            startTime,
            durationMinutes,
            null
        );

    [Fact]
    public void Create_TimedExam_ReturnsEventWithTrimmedTitleAndLocation()
    {
        var exam = _sut.Create(
            CalendarEventKind.Exam,
            "  Algorithms exam ",
            CourseId,
            null,
            Date,
            Ten,
            120,
            " Audimax "
        );

        Assert.Equal(CalendarEventKind.Exam, exam.Kind);
        Assert.Equal("Algorithms exam", exam.Title);
        Assert.Equal("Audimax", exam.Location);
        Assert.Equal(CourseId, exam.CourseId);
        Assert.Equal(Date, exam.Date);
        Assert.Equal(Ten, exam.StartTime);
        Assert.Equal(120, exam.DurationMinutes);
        Assert.Equal(7, exam.Id.Version);
        Assert.Equal(Now, exam.CreatedAt);
    }

    [Fact]
    public void Create_AllDayExam_HasNoTimeAndNoDuration()
    {
        var exam = Exam(null, null);

        Assert.Null(exam.StartTime);
        Assert.Null(exam.DurationMinutes);
    }

    [Fact]
    public void Create_ExamWithStartButNoDuration_Throws()
    {
        Assert.Throws<CalendarEventValidationException>(() => Exam(Ten, null));
    }

    [Fact]
    public void Create_ExamWithDurationButNoStart_Throws()
    {
        Assert.Throws<CalendarEventValidationException>(() => Exam(null, 120));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(721)]
    public void Create_ExamWithDurationOutOfRange_Throws(int durationMinutes)
    {
        Assert.Throws<CalendarEventValidationException>(() => Exam(Ten, durationMinutes));
    }

    [Fact]
    public void Create_ExamEndingAtMidnight_Succeeds()
    {
        Assert.Equal(60, Exam(new TimeOnly(23, 0), 60).DurationMinutes);
    }

    [Fact]
    public void Create_ExamEndingAfterMidnight_Throws()
    {
        Assert.Throws<CalendarEventValidationException>(() => Exam(new TimeOnly(23, 30), 60));
    }

    [Fact]
    public void Create_DeadlineWithDueTime_DropsSeconds()
    {
        Assert.Equal(new TimeOnly(23, 59), Deadline(new TimeOnly(23, 59, 59)).StartTime);
    }

    [Fact]
    public void Create_DeadlineWithoutTime_IsAllDay()
    {
        Assert.Null(Deadline(null).StartTime);
    }

    [Fact]
    public void Create_DeadlineWithDuration_Throws()
    {
        Assert.Throws<CalendarEventValidationException>(() => Deadline(new TimeOnly(12, 0), 30));
    }

    [Fact]
    public void Create_WithUnknownKind_Throws()
    {
        Assert.Throws<CalendarEventValidationException>(() =>
            _sut.Create((CalendarEventKind)0, "Exam", null, null, Date, null, null, null)
        );
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutTitle_Throws(string? title)
    {
        Assert.Throws<CalendarEventValidationException>(() =>
            _sut.Create(CalendarEventKind.Exam, title!, null, null, Date, null, null, null)
        );
    }

    [Fact]
    public void Create_WithTooLongTitleOrLocation_Throws()
    {
        Assert.Throws<CalendarEventValidationException>(() =>
            _sut.Create(
                CalendarEventKind.Exam,
                new string('t', CalendarEvent.TitleMaxLength + 1),
                null,
                null,
                Date,
                null,
                null,
                null
            )
        );
        Assert.Throws<CalendarEventValidationException>(() =>
            _sut.Create(
                CalendarEventKind.Exam,
                "Exam",
                null,
                null,
                Date,
                null,
                null,
                new string('l', CalendarEvent.LocationMaxLength + 1)
            )
        );
    }

    [Fact]
    public void Create_WithCourseAndSemester_Throws()
    {
        Assert.Throws<CalendarEventValidationException>(() =>
            _sut.Create(
                CalendarEventKind.Exam,
                "Exam",
                CourseId,
                Guid.NewGuid(),
                Date,
                null,
                null,
                null
            )
        );
    }

    [Fact]
    public void Update_TurnsAnExamIntoADeadlineAndKeepsIdentity()
    {
        var exam = Exam(Ten, 120);
        _timeProvider.UtcNow = Now.AddHours(1);

        var deadline = _sut.Update(
            exam,
            CalendarEventKind.Deadline,
            "Project report",
            null,
            null,
            Date.AddDays(3),
            new TimeOnly(23, 59),
            null,
            ""
        );

        Assert.Equal(exam.Id, deadline.Id);
        Assert.Equal(CalendarEventKind.Deadline, deadline.Kind);
        Assert.Equal("Project report", deadline.Title);
        Assert.Null(deadline.CourseId);
        Assert.Equal(Date.AddDays(3), deadline.Date);
        Assert.Null(deadline.DurationMinutes);
        Assert.Null(deadline.Location);
        Assert.Equal(Now, deadline.CreatedAt);
        Assert.Equal(Now.AddHours(1), deadline.UpdatedAt);
    }

    [Fact]
    public void Update_KeepingTheExamDurationOnADeadline_Throws()
    {
        var exam = Exam(Ten, 120);

        Assert.Throws<CalendarEventValidationException>(() =>
            _sut.Update(
                exam,
                CalendarEventKind.Deadline,
                exam.Title,
                null,
                null,
                Date,
                Ten,
                120,
                null
            )
        );
    }
}
