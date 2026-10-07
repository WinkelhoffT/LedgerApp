namespace StudyHub.Shared.CalendarEvents;

/// <param name="EndTime">Start plus duration for a timed exam; <c>null</c> otherwise.</param>
/// <param name="OwnerName">Name of the linked course or semester, if any.</param>
/// <param name="Color">Color of the linked course; <c>null</c> for semester events and events without a course.</param>
public sealed record CalendarEventDto(
    Guid Id,
    CalendarEventKind Kind,
    string Title,
    Guid? CourseId,
    Guid? SemesterId,
    DateOnly Date,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int? DurationMinutes,
    string? Location,
    string? OwnerName,
    string? Color,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
