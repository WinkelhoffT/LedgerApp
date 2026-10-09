namespace StudyHub.Shared.PracticeExams;

/// <summary>An attempt with its answers and the criteria ticked while self-grading.</summary>
public sealed record StoredPracticeExamAttempt(
    PracticeExamAttempt Attempt,
    IReadOnlyList<PracticeExamAnswer> Answers,
    IReadOnlyList<PracticeExamAnswerCriterion> MetCriteria
);
