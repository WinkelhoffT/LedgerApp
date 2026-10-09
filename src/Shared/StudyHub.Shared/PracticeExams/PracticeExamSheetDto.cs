namespace StudyHub.Shared.PracticeExams;

/// <summary>
/// The exam sheet of an attempt in progress: tasks and saved answers. Contains no solutions,
/// rationales, rubrics or correctness, so nothing gives the answers away before submission.
/// </summary>
/// <param name="DueAt">End of the time limit; <c>null</c> without one.</param>
public sealed record PracticeExamSheetDto(
    Guid AttemptId,
    Guid ExamId,
    string Title,
    PracticeExamLevel Level,
    int DurationMinutes,
    DateTime StartedAt,
    DateTime? DueAt,
    int MaxPoints,
    IReadOnlyList<PracticeExamSheetTaskDto> Tasks
);
