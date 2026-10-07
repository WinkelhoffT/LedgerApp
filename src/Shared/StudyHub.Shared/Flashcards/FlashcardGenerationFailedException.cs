namespace StudyHub.Shared.Flashcards;

public sealed class FlashcardGenerationFailedException(
    FlashcardGenerationFailureReason reason,
    string message,
    Exception? innerException = null) : Exception(message, innerException)
{
    public FlashcardGenerationFailureReason Reason { get; } = reason;
}
