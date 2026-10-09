namespace StudyHub.Shared.PracticeExams;

public sealed class PracticeExamTimeOverException(Guid attemptId)
    : Exception("The time for this attempt is over; answers can no longer be changed.")
{
    public Guid AttemptId { get; } = attemptId;
}
