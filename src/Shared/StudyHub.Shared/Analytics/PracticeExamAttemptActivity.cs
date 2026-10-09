namespace StudyHub.Shared.Analytics;

/// <summary>A submitted practice exam attempt as the study time statistics read it.</summary>
/// <param name="CourseId">The exam's course, or the course of the deck it was generated from.</param>
public sealed record PracticeExamAttemptActivity(
    DateTime StartedAt,
    DateTime SubmittedAt,
    int ExamDurationMinutes,
    Guid? CourseId
);
