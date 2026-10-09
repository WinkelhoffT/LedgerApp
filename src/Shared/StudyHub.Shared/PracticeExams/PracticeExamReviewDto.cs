namespace StudyHub.Shared.PracticeExams;

/// <summary>A submitted attempt with solutions, rationales, rubrics and points.</summary>
/// <param name="OpenTaskCount">Answered open tasks the student grades with the rubric.</param>
/// <param name="GradedOpenTaskCount">Of those, the ones graded so far.</param>
/// <param name="AwardedPoints">Set once every task has points.</param>
public sealed record PracticeExamReviewDto(
    Guid AttemptId,
    Guid ExamId,
    string Title,
    PracticeExamLevel Level,
    PracticeExamAttemptStatus Status,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    DateTime? GradedAt,
    int MaxPoints,
    int? AwardedPoints,
    int? Percent,
    int OpenTaskCount,
    int GradedOpenTaskCount,
    IReadOnlyList<PracticeExamReviewTaskDto> Tasks
);
