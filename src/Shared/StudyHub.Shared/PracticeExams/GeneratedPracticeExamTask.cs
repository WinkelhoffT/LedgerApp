namespace StudyHub.Shared.PracticeExams;

/// <summary>
/// One task as returned by the AI. Untrusted: the Domain validator normalizes it and drops it when
/// it breaks a task rule.
/// </summary>
/// <param name="Kind"><c>null</c> when the AI returned an unknown kind.</param>
/// <param name="SourceId">The number of the <see cref="PracticeExamSource"/> the task is based on.</param>
public sealed record GeneratedPracticeExamTask(
    PracticeExamTaskKind? Kind,
    string Text,
    int Points,
    IReadOnlyList<GeneratedPracticeExamOption> Options,
    IReadOnlyList<GeneratedPracticeExamCriterion> Criteria,
    string Solution,
    int? SourceId
);
