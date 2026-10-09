namespace StudyHub.Shared.PracticeExams;

public sealed class PracticeExamTaskNotFoundException(Guid taskId)
    : Exception($"Practice exam task '{taskId}' was not found.")
{
    public Guid TaskId { get; } = taskId;
}
