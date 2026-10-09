namespace StudyHub.Shared.PracticeExams;

/// <summary>One option of a single-choice task.</summary>
/// <param name="Rationale">Why the option is right or wrong; shown after submission.</param>
public sealed record PracticeExamOption(
    Guid Id,
    Guid TaskId,
    int Position,
    string Text,
    string Rationale,
    bool IsCorrect
)
{
    public const int TextMaxLength = 500;
    public const int RationaleMaxLength = 1_000;
}
