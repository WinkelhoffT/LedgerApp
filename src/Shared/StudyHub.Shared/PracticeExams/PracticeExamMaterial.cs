namespace StudyHub.Shared.PracticeExams;

/// <summary>The material a practice exam is generated from.</summary>
/// <param name="SourceName">The course or deck name, used in the exam title and the prompt.</param>
/// <param name="Length">Characters counted against <see cref="GeneratePracticeExamRequest.MaxMaterialLength"/>.</param>
/// <param name="OmittedCount">Cards left out because the deck is over the limit.</param>
public sealed record PracticeExamMaterial(
    string SourceName,
    IReadOnlyList<PracticeExamSource> Sources,
    int Length,
    int OmittedCount
);
