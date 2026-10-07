namespace StudyHub.Shared.Flashcards;

/// <summary>The answered card is not the one the study queue shows next, e.g. after a repeated submit.</summary>
public sealed class FlashcardNotDueException(Guid flashcardId)
    : Exception($"Flashcard '{flashcardId}' is not due for study right now.")
{
    public Guid FlashcardId { get; } = flashcardId;
}
