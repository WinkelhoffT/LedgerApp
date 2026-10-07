using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Business;

public sealed class CalendarEventOrchestrator(
    ICalendarEventRepository eventRepository,
    ICalendarEventLifecycle eventLifecycle,
    ICourseRepository courseRepository,
    ISemesterRepository semesterRepository,
    ICalendarPeriodProvider periodProvider
) : ICalendarEventOrchestrator
{
    public async Task<CalendarEventDto> CreateAsync(
        CreateCalendarEventRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var calendarEvent = eventLifecycle.Create(
            request.Kind,
            request.Title,
            request.CourseId,
            request.SemesterId,
            request.Date,
            request.StartTime,
            request.DurationMinutes,
            request.Location
        );

        var course = await GetAssignableCourseAsync(
            calendarEvent.CourseId,
            currentCourseId: null,
            cancellationToken
        );
        var semester = await GetAssignableSemesterAsync(
            calendarEvent.SemesterId,
            currentSemesterId: null,
            cancellationToken
        );

        await eventRepository.AddAsync(calendarEvent, cancellationToken);
        await eventRepository.SaveChangesAsync(cancellationToken);

        return CalendarEventMapper.ToDto(calendarEvent, course, semester);
    }

    public async Task<CalendarEventDto> UpdateAsync(
        UpdateCalendarEventRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var calendarEvent = await GetExistingEventAsync(request.Id, cancellationToken);
        var updated = eventLifecycle.Update(
            calendarEvent,
            request.Kind,
            request.Title,
            request.CourseId,
            request.SemesterId,
            request.Date,
            request.StartTime,
            request.DurationMinutes,
            request.Location
        );

        var course = await GetAssignableCourseAsync(
            updated.CourseId,
            calendarEvent.CourseId,
            cancellationToken
        );
        var semester = await GetAssignableSemesterAsync(
            updated.SemesterId,
            calendarEvent.SemesterId,
            cancellationToken
        );

        eventRepository.Update(updated);
        await eventRepository.SaveChangesAsync(cancellationToken);

        return CalendarEventMapper.ToDto(updated, course, semester);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var calendarEvent = await GetExistingEventAsync(id, cancellationToken);

        eventRepository.Remove(calendarEvent);
        await eventRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UpcomingCalendarEventDto>> GetUpcomingAsync(
        CancellationToken cancellationToken = default
    )
    {
        var today = periodProvider.GetToday();
        var events = await eventRepository.GetUpcomingAsync(
            today,
            UpcomingCalendarEventDto.MaxCount,
            cancellationToken
        );
        if (events.Count == 0)
        {
            return [];
        }

        // Archived courses and semesters are included, so their events keep their name and color.
        var courses = (await courseRepository.GetAllAsync(cancellationToken)).ToDictionary(c =>
            c.Id
        );
        var semesters = (await semesterRepository.GetAllAsync(cancellationToken)).ToDictionary(s =>
            s.Id
        );

        return events
            .Select(e => new UpcomingCalendarEventDto(
                CalendarEventMapper.ToDto(
                    e,
                    e.CourseId is { } courseId ? courses.GetValueOrDefault(courseId) : null,
                    e.SemesterId is { } semesterId ? semesters.GetValueOrDefault(semesterId) : null
                ),
                e.Date.DayNumber - today.DayNumber
            ))
            .ToList();
    }

    private async Task<CalendarEvent> GetExistingEventAsync(
        Guid id,
        CancellationToken cancellationToken
    ) =>
        await eventRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new CalendarEventNotFoundException(id);

    // An archived course cannot be newly linked; an event already linked to it may keep the link.
    private async Task<Course?> GetAssignableCourseAsync(
        Guid? courseId,
        Guid? currentCourseId,
        CancellationToken cancellationToken
    )
    {
        if (courseId is not { } id)
        {
            return null;
        }

        var course =
            await courseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new CourseNotFoundException(id);
        if (course.IsArchived && id != currentCourseId)
        {
            throw new CourseArchivedException(id);
        }

        return course;
    }

    // An archived semester cannot be newly linked; an event already linked to it may keep the link.
    private async Task<Semester?> GetAssignableSemesterAsync(
        Guid? semesterId,
        Guid? currentSemesterId,
        CancellationToken cancellationToken
    )
    {
        if (semesterId is not { } id)
        {
            return null;
        }

        var semester =
            await semesterRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new SemesterNotFoundException(id);
        if (semester.IsArchived && id != currentSemesterId)
        {
            throw new SemesterArchivedException(id);
        }

        return semester;
    }
}
