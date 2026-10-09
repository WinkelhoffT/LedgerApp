namespace StudyHub.Shared.StudySessions;

public sealed record CreateStudySessionRequest(
    string Title,
    Guid? CourseId,
    Guid? SemesterId,
    DateOnly Date,
    TimeOnly StartTime,
    int DurationMinutes,
    string? Location);
