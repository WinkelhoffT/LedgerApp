namespace StudyHub.Shared.StudySessions;

/// <param name="EndTime">Start plus duration; a session that ends at midnight has <c>00:00</c>.</param>
/// <param name="OwnerName">Name of the linked course or semester, if any.</param>
/// <param name="Color">Color of the linked course; <c>null</c> for semester sessions and sessions without a course.</param>
/// <param name="IsCompleted">Whether the session was marked as done.</param>
/// <param name="CompletedAt">UTC instant the session was marked as done.</param>
/// <param name="ActualDurationMinutes">How long the student actually studied; <c>null</c> while the session is not done.</param>
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
    bool IsCompleted,
    DateTime? CompletedAt,
    int? ActualDurationMinutes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
