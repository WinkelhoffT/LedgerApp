namespace StudyHub.Shared.PracticeExams;

/// <param name="CourseId">Required for <see cref="PracticeExamSourceKind.Course"/>.</param>
/// <param name="NoteIds">Selects some of the course's notes; <c>null</c> uses all of them.</param>
/// <param name="DeckId">Required for <see cref="PracticeExamSourceKind.Deck"/>.</param>
/// <param name="Model">Model id from <c>GET api/practice-exams/models</c>; <c>null</c> uses the configured default.</param>
public sealed record GeneratePracticeExamRequest(
    PracticeExamSourceKind SourceKind,
    Guid? CourseId,
    IReadOnlyList<Guid>? NoteIds,
    Guid? DeckId,
    PracticeExamLevel Level,
    int DurationMinutes,
    string? FocusHint,
    string? Model = null
)
{
    public const int DefaultDurationMinutes = 60;
    public const int FocusHintMaxLength = 500;

    /// <summary>
    /// Characters of material sent to the AI: a note's title and Markdown, or a card's front, back
    /// and tags.
    /// </summary>
    public const int MaxMaterialLength = 150_000;

    public static IReadOnlyList<int> AllowedDurations { get; } = [30, 45, 60, 90, 120];
}
