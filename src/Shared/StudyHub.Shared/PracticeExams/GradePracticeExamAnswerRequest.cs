namespace StudyHub.Shared.PracticeExams;

/// <param name="MetCriterionIds">The rubric criteria the answer meets; empty gives 0 points.</param>
public sealed record GradePracticeExamAnswerRequest(IReadOnlyList<Guid> MetCriterionIds);
