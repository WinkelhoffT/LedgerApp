namespace StudyHub.Shared.PracticeExams;

/// <param name="Number">The task's number within the attempt, starting at 1.</param>
/// <param name="Solution">The explanation (single choice) or the model solution (open task).</param>
/// <param name="Options">Single choice only; empty for an open task.</param>
/// <param name="Criteria">Open task only; empty for a single-choice task.</param>
/// <param name="AwardedPoints"><c>null</c> while an answered open task is not self-graded yet.</param>
/// <param name="Source">The note or card the task is based on, if Claude named one.</param>
/// <param name="IsExcluded">Marked as flawed after this attempt started.</param>
public sealed record PracticeExamReviewTaskDto(
    Guid TaskId,
    int Number,
    PracticeExamTaskKind Kind,
    string Text,
    int Points,
    string Solution,
    IReadOnlyList<PracticeExamReviewOptionDto> Options,
    Guid? SelectedOptionId,
    IReadOnlyList<PracticeExamReviewCriterionDto> Criteria,
    string? AnswerText,
    int? AwardedPoints,
    PracticeExamSourceReferenceDto? Source,
    bool IsExcluded
);
