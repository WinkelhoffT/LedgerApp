namespace StudyHub.Shared.PracticeExams;

/// <summary>A practice exam as listed and shown on its cover page, without its tasks.</summary>
/// <param name="SourceName">The course or deck name.</param>
/// <param name="TaskCount">Tasks a new attempt contains (excluded tasks not counted).</param>
/// <param name="TotalPoints">Points a new attempt can reach (excluded tasks not counted).</param>
/// <param name="BestAttempt">The graded attempt with the highest percentage, if any.</param>
/// <param name="OpenAttemptId">The attempt in progress, which starting resumes.</param>
public sealed record PracticeExamDto(
    Guid Id,
    string Title,
    PracticeExamLevel Level,
    int DurationMinutes,
    PracticeExamSourceKind SourceKind,
    Guid? CourseId,
    Guid? DeckId,
    string SourceName,
    string Model,
    string? FocusHint,
    int TaskCount,
    int TotalPoints,
    int ExcludedTaskCount,
    int AttemptCount,
    PracticeExamAttemptSummaryDto? BestAttempt,
    Guid? OpenAttemptId,
    bool IsArchived,
    DateTime CreatedAt
);
