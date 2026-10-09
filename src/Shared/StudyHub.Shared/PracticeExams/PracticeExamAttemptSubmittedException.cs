namespace StudyHub.Shared.PracticeExams;

public sealed class PracticeExamAttemptSubmittedException(Guid attemptId)
    : Exception("This attempt has already been submitted; its answers can no longer be changed.")
{
    public Guid AttemptId { get; } = attemptId;
}
