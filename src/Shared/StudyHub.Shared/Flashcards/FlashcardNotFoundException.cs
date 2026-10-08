namespace StudyHub.Shared.Flashcards;

public sealed class FlashcardNotFoundException(Guid flashcardId) : Exception($"Flashcard '{flashcardId}' was not found.")
{
    public Guid FlashcardId { get; } = flashcardId;
}
