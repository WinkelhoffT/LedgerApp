namespace StudyHub.Shared.PracticeExams;

/// <summary>
/// The answer to one task within an attempt. The answer rows created when the attempt starts fix its
/// task set. <see cref="AwardedPoints"/> stays <c>null</c> until the task is graded.
/// </summary>
public sealed record PracticeExamAnswer(
    Guid Id,
    Guid AttemptId,
    Guid TaskId,
    Guid? SelectedOptionId,
    string? AnswerText,
    int? AwardedPoints,
    DateTime UpdatedAt
)
{
    public const int AnswerTextMaxLength = 10_000;
}
