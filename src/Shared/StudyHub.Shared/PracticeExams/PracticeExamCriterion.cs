namespace StudyHub.Shared.PracticeExams;

/// <summary>One rubric item of an open task.</summary>
public sealed record PracticeExamCriterion(
    Guid Id,
    Guid TaskId,
    int Position,
    string Description,
    int Points
)
{
    public const int DescriptionMaxLength = 500;
}
