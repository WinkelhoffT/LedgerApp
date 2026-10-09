namespace StudyHub.Shared.PracticeExams;

public sealed class PracticeExamArchivedException(Guid examId)
    : Exception($"Practice exam '{examId}' is archived.")
{
    public Guid ExamId { get; } = examId;
}
