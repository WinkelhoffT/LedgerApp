namespace StudyHub.Shared.PracticeExams;

/// <summary>A task with its options (single choice) or criteria (open task), ordered by position.</summary>
public sealed record StoredPracticeExamTask(
    PracticeExamTask Task,
    IReadOnlyList<PracticeExamOption> Options,
    IReadOnlyList<PracticeExamCriterion> Criteria
);
