namespace StudyHub.Shared.PracticeExams;

/// <param name="Number">The task's number within the attempt, starting at 1.</param>
/// <param name="Options">Single choice only; empty for an open task.</param>
public sealed record PracticeExamSheetTaskDto(
    Guid TaskId,
    int Number,
    PracticeExamTaskKind Kind,
    string Text,
    int Points,
    IReadOnlyList<PracticeExamSheetOptionDto> Options,
    Guid? SelectedOptionId,
    string? AnswerText
);
