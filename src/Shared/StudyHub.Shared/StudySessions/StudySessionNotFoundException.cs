namespace StudyHub.Shared.StudySessions;

public sealed class StudySessionNotFoundException(Guid studySessionId)
    : Exception($"Study session '{studySessionId}' was not found.")
{
    public Guid StudySessionId { get; } = studySessionId;
}
