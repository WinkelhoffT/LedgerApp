namespace StudyHub.Shared.CalendarEvents;

public sealed record UpdateCalendarEventRequest(
    Guid Id,
    CalendarEventKind Kind,
    string Title,
    Guid? CourseId,
    Guid? SemesterId,
    DateOnly Date,
    TimeOnly? StartTime,
    int? DurationMinutes,
    string? Location);
