namespace StudyHub.Shared.StudySessions;

/// <summary>
/// A planned block of study time in the calendar. Date and start are local wall-clock values, so a
/// session planned for 09:00 stays at 09:00 across daylight-saving changes. A session belongs to at
/// most one of a course or a semester, or to neither, and ends on the day it starts.
/// </summary>
public sealed record StudySession(
    Guid Id,
    string Title,
    Guid? CourseId,
    Guid? SemesterId,
    DateOnly Date,
    TimeOnly StartTime,
    int DurationMinutes,
    string? Location,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public const int TitleMaxLength = 200;
    public const int LocationMaxLength = 200;
    public const int MinDurationMinutes = 5;
    public const int MaxDurationMinutes = 720;
    public const int DefaultDurationMinutes = 60;
}
