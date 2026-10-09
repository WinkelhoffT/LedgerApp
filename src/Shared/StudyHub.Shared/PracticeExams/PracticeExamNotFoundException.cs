namespace StudyHub.Shared.PracticeExams;

public sealed class PracticeExamNotFoundException(Guid examId)
    : Exception($"Practice exam '{examId}' was not found.")
{
    public Guid ExamId { get; } = examId;
}
