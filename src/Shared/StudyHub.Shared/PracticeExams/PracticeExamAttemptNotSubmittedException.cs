namespace StudyHub.Shared.PracticeExams;

public sealed class PracticeExamAttemptNotSubmittedException(Guid attemptId)
    : Exception("This attempt has not been submitted yet; solutions are shown after submission.")
{
    public Guid AttemptId { get; } = attemptId;
}
