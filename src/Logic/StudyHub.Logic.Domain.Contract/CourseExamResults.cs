namespace StudyHub.Logic.Domain.Contract;

/// <summary>The graded practice exam attempts of one course.</summary>
/// <param name="LatestPercent">Result of the most recently graded attempt, in percent of its points; <c>null</c> without graded attempts.</param>
/// <param name="BestPercent">Best result, in percent of its points; <c>null</c> without graded attempts.</param>
public sealed record CourseExamResults(int? LatestPercent, int? BestPercent, int GradedAttempts);
