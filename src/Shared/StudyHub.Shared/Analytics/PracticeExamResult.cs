namespace StudyHub.Shared.Analytics;

/// <summary>A graded attempt at a practice exam that is not archived.</summary>
/// <param name="CourseId">The exam's course, or the course of the deck it was generated from.</param>
public sealed record PracticeExamResult(
    Guid? CourseId,
    DateTime GradedAt,
    int AwardedPoints,
    int MaxPoints
);
