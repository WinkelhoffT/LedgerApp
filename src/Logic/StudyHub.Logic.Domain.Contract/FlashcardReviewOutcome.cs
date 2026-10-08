using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>The rescheduled card and the review-log entry for one answer.</summary>
public sealed record FlashcardReviewOutcome(Flashcard Card, FlashcardReview Review);
