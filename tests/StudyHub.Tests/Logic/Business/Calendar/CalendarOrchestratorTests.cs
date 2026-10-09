using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Business.Calendar;

public class CalendarOrchestratorTests
{
    // 2026-10-08 00:30 in Berlin, while it is still 2026-10-07 in UTC.
    private static readonly DateTime Now = new(2026, 10, 7, 22, 30, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 10, 8);

    private readonly Mock<IStudySessionRepository> _sessionRepository = new();
    private readonly Mock<ICalendarEventRepository> _eventRepository = new();
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly Mock<ISemesterRepository> _semesterRepository = new();
    private readonly CalendarOrchestrator _sut;

    public CalendarOrchestratorTests()
    {
        _sut = new CalendarOrchestrator(
            _sessionRepository.Object,
            _eventRepository.Object,
            _courseRepository.Object,
            _semesterRepository.Object,
            new CalendarPeriodProvider(
                new CalendarOptions { TimeZone = "Europe/Berlin" },
                new FixedTimeProvider(Now)
            ),
            new CalendarLaneProcessor()
        );

        _sessionRepository
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), default))
            .ReturnsAsync([]);
        _eventRepository
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), default))
            .ReturnsAsync([]);
    }

    private static StudySession Session(
        string title,
        DateOnly date,
        int hour,
        int durationMinutes = 60,
        Guid? courseId = null,
        Guid? semesterId = null
    ) =>
        new(
            Guid.NewGuid(),
            title,
            courseId,
            semesterId,
            date,
            new TimeOnly(hour, 0),
            durationMinutes,
            null,
            null,
            null,
            Now,
            Now
        );

    private void SetupSessions(DateOnly from, DateOnly to, params StudySession[] sessions) =>
        _sessionRepository
            .Setup(r => r.GetByDateRangeAsync(from, to, default))
            .ReturnsAsync(sessions);

    private static CalendarEvent Event(
        string title,
        DateOnly date,
        CalendarEventKind kind = CalendarEventKind.Exam,
        int? hour = null,
        int? durationMinutes = null,
        Guid? courseId = null
    ) =>
        new(
            Guid.NewGuid(),
            kind,
            title,
            courseId,
            null,
            date,
            hour is { } h ? new TimeOnly(h, 0) : null,
            durationMinutes,
            null,
            Now,
            Now
        );

    private void SetupEvents(DateOnly from, DateOnly to, params CalendarEvent[] events) =>
        _eventRepository.Setup(r => r.GetByDateRangeAsync(from, to, default)).ReturnsAsync(events);

    [Fact]
    public async Task GetMonthAsync_ReturnsWholeWeeksWithSessionsGroupedPerDayByStart()
    {
        // October 2026 starts on a Thursday and ends on a Saturday.
        var from = new DateOnly(2026, 9, 28);
        var to = new DateOnly(2026, 11, 1);
        SetupSessions(
            from,
            to,
            Session("Late", Today, 14),
            Session("Early", Today, 9),
            Session("Neighbour month", from, 10)
        );

        var month = await _sut.GetMonthAsync(2026, 10);

        Assert.Equal(2026, month.Year);
        Assert.Equal(10, month.Month);
        Assert.Equal(Today, month.Today);
        Assert.Equal(35, month.Days.Count);
        Assert.Equal(from, month.Days[0].Date);
        Assert.Equal(to, month.Days[^1].Date);
        Assert.Equal(["Neighbour month"], month.Days[0].Sessions.Select(s => s.Session.Title));
        Assert.Equal(
            ["Early", "Late"],
            month.Days.Single(d => d.Date == Today).Sessions.Select(s => s.Session.Title)
        );
    }

    [Fact]
    public async Task GetMonthAsync_IgnoresSessionsOutsideThePeriod()
    {
        SetupSessions(
            new DateOnly(2026, 9, 28),
            new DateOnly(2026, 11, 1),
            Session("Elsewhere", new DateOnly(2026, 12, 24), 9)
        );

        var month = await _sut.GetMonthAsync(2026, 10);

        Assert.All(month.Days, day => Assert.Empty(day.Sessions));
    }

    [Fact]
    public async Task GetMonthAsync_ResolvesCourseAndSemesterOwners()
    {
        var course = new Course(
            Guid.NewGuid(),
            "Algorithms",
            null,
            "#2563eb",
            Guid.NewGuid(),
            IsArchived: false,
            Now,
            Now
        );
        var archivedCourse = new Course(
            Guid.NewGuid(),
            "Old course",
            null,
            "#16a34a",
            Guid.NewGuid(),
            IsArchived: true,
            Now,
            Now
        );
        var semester = new Semester(
            Guid.NewGuid(),
            "Winter 2026/27",
            new DateOnly(2026, 10, 1),
            new DateOnly(2027, 3, 31),
            false,
            Now,
            Now
        );
        _courseRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([course, archivedCourse]);
        _semesterRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([semester]);
        SetupSessions(
            new DateOnly(2026, 9, 28),
            new DateOnly(2026, 11, 1),
            Session("Course session", Today, 8, courseId: course.Id),
            Session("Archived course session", Today, 10, courseId: archivedCourse.Id),
            Session("Semester session", Today, 12, semesterId: semester.Id),
            Session("Free session", Today, 14)
        );

        var month = await _sut.GetMonthAsync(2026, 10);

        var sessions = month
            .Days.Single(d => d.Date == Today)
            .Sessions.Select(s => s.Session)
            .ToList();
        Assert.Equal(
            [
                ("Algorithms", "#2563eb"),
                ("Old course", "#16a34a"),
                ("Winter 2026/27", null),
                (null, null),
            ],
            sessions.Select(s => (s.OwnerName, s.Color))
        );
    }

    [Fact]
    public async Task GetMonthAsync_WithoutLinkedSessions_DoesNotLoadCoursesOrSemesters()
    {
        SetupSessions(
            new DateOnly(2026, 9, 28),
            new DateOnly(2026, 11, 1),
            Session("Free session", Today, 9)
        );

        await _sut.GetMonthAsync(2026, 10);

        _courseRepository.Verify(r => r.GetAllAsync(default), Times.Never);
        _semesterRepository.Verify(r => r.GetAllAsync(default), Times.Never);
    }

    [Fact]
    public async Task GetWeekAsync_ReturnsIsoWeekWithHoursAndOverlapLanes()
    {
        var from = new DateOnly(2026, 10, 5);
        var to = new DateOnly(2026, 10, 11);
        SetupSessions(
            from,
            to,
            Session("A", Today, 6, durationMinutes: 120),
            Session("B", Today, 7),
            Session("Sunday", to, 21, durationMinutes: 90)
        );

        var week = await _sut.GetWeekAsync(to);

        Assert.Equal(from, week.Start);
        Assert.Equal(to, week.End);
        Assert.Equal(41, week.IsoWeek);
        Assert.Equal(Today, week.Today);
        Assert.Equal(6, week.StartHour);
        Assert.Equal(23, week.EndHour);
        Assert.Equal(7, week.Days.Count);
        Assert.Equal(
            [("A", 0, 2), ("B", 1, 2)],
            week.Days.Single(d => d.Date == Today)
                .Sessions.Select(s => (s.Session.Title, s.Lane, s.LaneCount))
        );
    }

    [Fact]
    public async Task GetTodayAsync_UsesTheCalendarTimeZone()
    {
        SetupSessions(Today, Today, Session("Graph review", Today, 9));

        var day = await _sut.GetTodayAsync();

        Assert.Equal(Today, day.Date);
        Assert.Equal("Graph review", Assert.Single(day.Sessions).Session.Title);
    }

    [Fact]
    public async Task GetMonthAsync_ListsEventsPerDayWithAllDayEventsFirst()
    {
        var course = new Course(
            Guid.NewGuid(),
            "Algorithms",
            null,
            "#2563eb",
            Guid.NewGuid(),
            IsArchived: false,
            Now,
            Now
        );
        _courseRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([course]);
        SetupEvents(
            new DateOnly(2026, 9, 28),
            new DateOnly(2026, 11, 1),
            Event("Sheet 3", Today, CalendarEventKind.Deadline, hour: 23),
            Event("Algorithms exam", Today, hour: 10, durationMinutes: 120, courseId: course.Id),
            Event("Project report", Today, CalendarEventKind.Deadline)
        );

        var month = await _sut.GetMonthAsync(2026, 10);

        var events = month.Days.Single(d => d.Date == Today).Events.Select(e => e.Event).ToList();
        Assert.Equal(["Project report", "Algorithms exam", "Sheet 3"], events.Select(e => e.Title));
        Assert.Equal(
            ("Algorithms", "#2563eb", new TimeOnly(12, 0)),
            (events[1].OwnerName, events[1].Color, events[1].EndTime)
        );
        Assert.Null(events[2].EndTime);
    }

    [Fact]
    public async Task GetWeekAsync_PlacesTimedExamsInTheLanesAndWidensTheHours()
    {
        var from = new DateOnly(2026, 10, 5);
        var to = new DateOnly(2026, 10, 11);
        SetupSessions(from, to, Session("Review", Today, 9, durationMinutes: 120));
        SetupEvents(
            from,
            to,
            Event("Exam", Today, hour: 10, durationMinutes: 60),
            Event("All-day exam", Today),
            Event("Early exam", to, hour: 6, durationMinutes: 90)
        );

        var week = await _sut.GetWeekAsync(Today);

        var day = week.Days.Single(d => d.Date == Today);
        Assert.Equal(
            ("Review", 0, 2),
            day.Sessions.Select(s => (s.Session.Title, s.Lane, s.LaneCount)).Single()
        );
        Assert.Equal(
            [("All-day exam", 0, 1), ("Exam", 1, 2)],
            day.Events.Select(e => (e.Event.Title, e.Lane, e.LaneCount))
        );
        Assert.Equal(6, week.StartHour);
    }

    [Fact]
    public async Task GetTodayAsync_IncludesTodaysEvents()
    {
        SetupEvents(Today, Today, Event("Sheet 3", Today, CalendarEventKind.Deadline, hour: 23));

        var day = await _sut.GetTodayAsync();

        Assert.Equal("Sheet 3", Assert.Single(day.Events).Event.Title);
    }
}
