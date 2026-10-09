namespace StudyHub.Shared.PracticeExams;

/// <summary>
/// One task of a practice exam. For an <see cref="PracticeExamTaskKind.Open"/> task,
/// <see cref="Points"/> is the sum of its criteria. At most one of <see cref="SourceNoteId"/> and
/// <see cref="SourceFlashcardId"/> is set.
/// </summary>
/// <param name="Position">Order within the exam, starting at 1.</param>
/// <param name="Solution">The explanation (single choice) or the model solution (open task).</param>
/// <param name="IsExcluded">Marked as flawed: left out of future attempts.</param>
public sealed record PracticeExamTask(
    Guid Id,
    Guid ExamId,
    int Position,
    PracticeExamTaskKind Kind,
    string Text,
    int Points,
    string Solution,
    Guid? SourceNoteId,
    Guid? SourceFlashcardId,
    bool IsExcluded,
    DateTime CreatedAt
)
{
    public const int TextMaxLength = 4_000;
    public const int SolutionMaxLength = 8_000;
    public const int MaxPoints = 30;
    public const int MinSingleChoicePoints = 1;
    public const int MaxSingleChoicePoints = 3;
    public const int SingleChoiceOptionCount = 4;
    public const int MinCriteria = 1;
    public const int MaxCriteria = 6;
}
