namespace StudyHub.Shared.Flashcards;

/// <summary>
/// A stored Basic card with its scheduling state. For a <see cref="FlashcardState.New"/> card,
/// <see cref="DueAt"/> is its position in the new-card queue (the order the cards were added in).
/// <see cref="EaseFactor"/> is in permille, as in Anki (2500 = 250 %).
/// </summary>
/// <param name="Tags">Space-separated, as in Anki; a single tag never contains whitespace.</param>
/// <param name="Step">Index into the learning or relearning steps while the card is in one of those states.</param>
public sealed record Flashcard(
    Guid Id,
    Guid DeckId,
    string Front,
    string Back,
    string? Tags,
    Guid? SourceNoteId,
    FlashcardState State,
    int Step,
    DateTime DueAt,
    int IntervalDays,
    int EaseFactor,
    int Reps,
    int Lapses,
    DateTime? LastReviewedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public const int TagsMaxLength = 600;
}
