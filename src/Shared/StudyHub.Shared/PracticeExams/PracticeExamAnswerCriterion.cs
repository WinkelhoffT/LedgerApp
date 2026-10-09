namespace StudyHub.Shared.PracticeExams;

/// <summary>A rubric criterion the student ticked as met when self-grading an open answer.</summary>
public sealed record PracticeExamAnswerCriterion(Guid AnswerId, Guid CriterionId);
