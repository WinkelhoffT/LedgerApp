namespace StudyHub.Shared.Flashcards;

public sealed class FlashcardDeckArchivedException(Guid deckId) : Exception($"Flashcard deck '{deckId}' is archived.")
{
    public Guid DeckId { get; } = deckId;
}
