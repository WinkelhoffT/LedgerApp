using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Business;

/// <summary>Maps stored exams and deadlines to the DTO the calendar and the event endpoints return.</summary>
internal static class CalendarEventMapper
{
    /// <param name="course">The event's course, if it has one.</param>
    /// <param name="semester">The event's semester, if it has one.</param>
    public static CalendarEventDto ToDto(
        CalendarEvent calendarEvent,
        Course? course,
        Semester? semester
    ) =>
        new(
            calendarEvent.Id,
            calendarEvent.Kind,
            calendarEvent.Title,
            calendarEvent.CourseId,
            calendarEvent.SemesterId,
            calendarEvent.Date,
            calendarEvent.StartTime,
            calendarEvent is { StartTime: { } start, DurationMinutes: { } duration }
                ? start.AddMinutes(duration)
                : null,
            calendarEvent.DurationMinutes,
            calendarEvent.Location,
            course?.Name ?? semester?.Name,
            course?.Color,
            calendarEvent.CreatedAt,
            calendarEvent.UpdatedAt
        );
}
