namespace StudyHub.Shared.PracticeExams;

/// <param name="AwardedPoints">Set once every task has points.</param>
/// <param name="Percent">Rounded percentage of <see cref="MaxPoints"/>; set with <see cref="AwardedPoints"/>.</param>
public sealed record PracticeExamAttemptSummaryDto(
    Guid Id,
    Guid ExamId,
    PracticeExamAttemptStatus Status,
    DateTime StartedAt,
    DateTime? DueAt,
    DateTime? SubmittedAt,
    DateTime? GradedAt,
    int MaxPoints,
    int? AwardedPoints,
    int? Percent
);
