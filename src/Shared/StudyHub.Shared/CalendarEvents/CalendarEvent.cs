namespace StudyHub.Shared.CalendarEvents;

/// <summary>
/// A fixed date in the calendar that the student works towards: an exam or a deadline. Date and
/// time are local wall-clock values like those of a study session. Without a start time the event
/// lasts all day; only a timed exam has a duration. An event belongs to at most one of a course or
/// a semester, or to neither.
/// </summary>
public sealed record CalendarEvent(
    Guid Id,
    CalendarEventKind Kind,
    string Title,
    Guid? CourseId,
    Guid? SemesterId,
    DateOnly Date,
    TimeOnly? StartTime,
    int? DurationMinutes,
    string? Location,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public const int TitleMaxLength = 200;
    public const int LocationMaxLength = 200;
    public const int MinDurationMinutes = 5;
    public const int MaxDurationMinutes = 720;
    public const int DefaultExamDurationMinutes = 120;
}
