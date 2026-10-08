namespace StudyHub.Shared.StudySessions;

/// <param name="EndTime">Start plus duration; a session that ends at midnight has <c>00:00</c>.</param>
/// <param name="OwnerName">Name of the linked course or semester, if any.</param>
/// <param name="Color">Color of the linked course; <c>null</c> for semester sessions and sessions without a course.</param>
public sealed record StudySessionDto(
    Guid Id,
    string Title,
    Guid? CourseId,
    Guid? SemesterId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int DurationMinutes,
    string? Location,
    string? OwnerName,
    string? Color,
    DateTime CreatedAt,
    DateTime UpdatedAt);
