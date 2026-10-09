namespace StudyHub.Shared.PracticeExams;

public sealed class PracticeExamAttemptNotFoundException(Guid attemptId)
    : Exception($"Practice exam attempt '{attemptId}' was not found.")
{
    public Guid AttemptId { get; } = attemptId;
}
