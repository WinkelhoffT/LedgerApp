namespace StudyHub.Shared.Flashcards;

public enum FlashcardGenerationFailureReason
{
    Unknown,
    Refused,
    Truncated,
    InvalidResponse,
    RateLimited,
    ServiceUnavailable,
    Unauthorized,
}
