using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Integration.Ai;

/// <param name="SourceName">The course or deck name.</param>
/// <param name="Sources">The numbered notes or cards the tasks are based on.</param>
public sealed record PracticeExamGenerationInput(
    string Model,
    PracticeExamLevel Level,
    int DurationMinutes,
    PracticeExamSourceKind SourceKind,
    string SourceName,
    IReadOnlyList<PracticeExamSource> Sources,
    string? FocusHint
);
