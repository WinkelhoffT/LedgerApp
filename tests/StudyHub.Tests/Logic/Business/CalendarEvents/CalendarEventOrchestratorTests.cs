using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Business.CalendarEvents;

public class CalendarEventOrchestratorTests
{
    // 2026-10-08 00:30 in Berlin, while it is still 2026-10-07 in UTC.
    private static readonly DateTime Now = new(2026, 10, 7, 22, 30, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 10, 8);
    private static readonly TimeOnly Ten = new(10, 0);

    private readonly Mock<ICalendarEventRepository> _eventRepository = new();
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly Mock<ISemesterRepository> _semesterRepository = new();
    private readonly CalendarEventOrchestrator _sut;

    public CalendarEventOrchestratorTests()
    {
        var timeProvider = new FixedTimeProvider(Now);
        _sut = new CalendarEventOrchestrator(
            _eventRepository.Object,
            new CalendarEventLifecycle(timeProvider),
            _courseRepository.Object,
            _semesterRepository.Object,
            new CalendarPeriodProvider(
                new CalendarOptions { TimeZone = "Europe/Berlin" },
                timeProvider
            )
        );
    }

    private CalendarEvent SetupEvent(Guid? courseId = null)
    {
        var calendarEvent = new CalendarEvent(
            Guid.NewGuid(),
            CalendarEventKind.Exam,
            "Algorithms exam",
            courseId,
            null,
            Today,
            Ten,
            120,
            null,
            Now,
            Now
        );
        _eventRepository
            .Setup(r => r.GetByIdAsync(calendarEvent.Id, default))
            .ReturnsAsync(calendarEvent);
        return calendarEvent;
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

    private static CreateCalendarEventRequest ExamRequest(
        Guid? courseId = null,
        Guid? semesterId = null,
        int? durationMinutes = 120
    ) =>
        new(
            CalendarEventKind.Exam,
            "Algorithms exam",
            courseId,
            semesterId,
            Today,
            Ten,
            durationMinutes,
            "Audimax"
        );

    private static UpdateCalendarEventRequest DeadlineRequest(Guid id, Guid? courseId = null) =>
        new(
            id,
            CalendarEventKind.Deadline,
            "Exercise sheet 3",
            courseId,
            null,
            Today.AddDays(2),
            new TimeOnly(23, 59),
            null,
            null
        );

    [Fact]
    public async Task CreateAsync_WithCourse_SavesEventAndReturnsCourseNameColorAndEnd()
    {
        var course = SetupCourse();

        var result = await _sut.CreateAsync(ExamRequest(course.Id));

        Assert.Equal(CalendarEventKind.Exam, result.Kind);
        Assert.Equal(("Algorithms", "#2563eb"), (result.OwnerName, result.Color));
        Assert.Equal(new TimeOnly(12, 0), result.EndTime);
        _eventRepository.Verify(
            r =>
                r.AddAsync(
                    It.Is<CalendarEvent>(e => e.CourseId == course.Id && e.DurationMinutes == 120),
                    default
                ),
            Times.Once
        );
        _eventRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithSemester_ReturnsSemesterNameWithoutColor()
    {
        var semester = SetupSemester();

        var result = await _sut.CreateAsync(ExamRequest(semesterId: semester.Id));

        Assert.Equal(("Winter 2026/27", (string?)null), (result.OwnerName, result.Color));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidTime_ThrowsWithoutSaving()
    {
        await Assert.ThrowsAsync<CalendarEventValidationException>(() =>
            _sut.CreateAsync(ExamRequest(durationMinutes: null))
        );
        _eventRepository.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownOrArchivedOwner_Throws()
    {
        var archivedCourse = SetupCourse(isArchived: true);
        var archivedSemester = SetupSemester(isArchived: true);

        await Assert.ThrowsAsync<CourseNotFoundException>(() =>
            _sut.CreateAsync(ExamRequest(Guid.NewGuid()))
        );
        await Assert.ThrowsAsync<CourseArchivedException>(() =>
            _sut.CreateAsync(ExamRequest(archivedCourse.Id))
        );
        await Assert.ThrowsAsync<SemesterNotFoundException>(() =>
            _sut.CreateAsync(ExamRequest(semesterId: Guid.NewGuid()))
        );
        await Assert.ThrowsAsync<SemesterArchivedException>(() =>
            _sut.CreateAsync(ExamRequest(semesterId: archivedSemester.Id))
        );
        _eventRepository.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_TurnsTheExamIntoADeadline()
    {
        var calendarEvent = SetupEvent();

        var result = await _sut.UpdateAsync(DeadlineRequest(calendarEvent.Id));

        Assert.Equal(calendarEvent.Id, result.Id);
        Assert.Equal(CalendarEventKind.Deadline, result.Kind);
        Assert.Null(result.EndTime);
        _eventRepository.Verify(
            r =>
                r.Update(
                    It.Is<CalendarEvent>(e => e.Id == calendarEvent.Id && e.DurationMinutes == null)
                ),
            Times.Once
        );
        _eventRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_KeepsAnAlreadyLinkedArchivedCourse()
    {
        var course = SetupCourse(isArchived: true);
        var calendarEvent = SetupEvent(course.Id);

        var result = await _sut.UpdateAsync(DeadlineRequest(calendarEvent.Id, course.Id));

        Assert.Equal(course.Id, result.CourseId);
    }

    [Fact]
    public async Task UpdateAsync_SwitchingToAnArchivedCourse_Throws()
    {
        var course = SetupCourse(isArchived: true);
        var calendarEvent = SetupEvent();

        await Assert.ThrowsAsync<CourseArchivedException>(() =>
            _sut.UpdateAsync(DeadlineRequest(calendarEvent.Id, course.Id))
        );
    }

    [Fact]
    public async Task UpdateAndDeleteAsync_WithUnknownEvent_Throw()
    {
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<CalendarEventNotFoundException>(() =>
            _sut.UpdateAsync(DeadlineRequest(id))
        );
        var ex = await Assert.ThrowsAsync<CalendarEventNotFoundException>(() =>
            _sut.DeleteAsync(id)
        );

        Assert.Equal(id, ex.CalendarEventId);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheEvent()
    {
        var calendarEvent = SetupEvent();

        await _sut.DeleteAsync(calendarEvent.Id);

        _eventRepository.Verify(r => r.Remove(calendarEvent), Times.Once);
        _eventRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetUpcomingAsync_ReturnsTheNextEventsFromTodayWithDaysUntil()
    {
        var course = new Course(
            Guid.NewGuid(),
            "Algorithms",
            null,
            "#2563eb",
            Guid.NewGuid(),
            IsArchived: true,
            Now,
            Now
        );
        _courseRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([course]);
        _semesterRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);
        _eventRepository
            .Setup(r => r.GetUpcomingAsync(Today, UpcomingCalendarEventDto.MaxCount, default))
            .ReturnsAsync([
                new CalendarEvent(
                    Guid.NewGuid(),
                    CalendarEventKind.Deadline,
                    "Sheet 3",
                    null,
                    null,
                    Today,
                    new TimeOnly(23, 59),
                    null,
                    null,
                    Now,
                    Now
                ),
                new CalendarEvent(
                    Guid.NewGuid(),
                    CalendarEventKind.Exam,
                    "Algorithms exam",
                    course.Id,
                    null,
                    Today.AddDays(12),
                    Ten,
                    120,
                    null,
                    Now,
                    Now
                ),
            ]);

        var upcoming = await _sut.GetUpcomingAsync();

        Assert.Equal(
            [("Sheet 3", 0), ("Algorithms exam", 12)],
            upcoming.Select(u => (u.Event.Title, u.DaysUntil))
        );
        Assert.Equal("Algorithms", upcoming[1].Event.OwnerName);
    }

    [Fact]
    public async Task GetUpcomingAsync_WithoutEvents_ReturnsEmptyWithoutLoadingOwners()
    {
        _eventRepository
            .Setup(r => r.GetUpcomingAsync(Today, UpcomingCalendarEventDto.MaxCount, default))
            .ReturnsAsync([]);

        Assert.Empty(await _sut.GetUpcomingAsync());
        _courseRepository.Verify(r => r.GetAllAsync(default), Times.Never);
    }
}
