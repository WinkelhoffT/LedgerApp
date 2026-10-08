namespace StudyHub.Shared.Flashcards;

/// <summary>A stored card as listed on the deck page.</summary>
/// <param name="DueDate">Study day the card is due on; <c>null</c> for new cards.</param>
public sealed record DeckCardDto(
    Guid Id,
    Guid DeckId,
    string Front,
    string Back,
    IReadOnlyList<string> Tags,
    FlashcardState State,
    DateOnly? DueDate,
    int IntervalDays,
    int Reps,
    int Lapses,
    Guid? SourceNoteId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
