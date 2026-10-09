namespace StudyHub.Shared.Flashcards;

public sealed class FlashcardDeckNotFoundException(Guid deckId)
    : Exception($"Flashcard deck '{deckId}' was not found.")
{
    public Guid DeckId { get; } = deckId;
}
