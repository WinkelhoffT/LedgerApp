namespace StudyHub.Shared.PracticeExams;

/// <summary>
/// One sitting of a practice exam. <see cref="MaxPoints"/> is fixed when the attempt starts;
/// <see cref="AwardedPoints"/> is set once every task has points.
/// </summary>
/// <param name="DueAt">End of the time limit; <c>null</c> for an attempt without one.</param>
public sealed record PracticeExamAttempt(
    Guid Id,
    Guid ExamId,
    DateTime StartedAt,
    DateTime? DueAt,
    DateTime? SubmittedAt,
    DateTime? GradedAt,
    int MaxPoints,
    int? AwardedPoints
);
