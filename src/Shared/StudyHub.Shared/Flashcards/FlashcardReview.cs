namespace StudyHub.Shared.Flashcards;

/// <summary>One answer given in a study session (Anki's review log).</summary>
public sealed record FlashcardReview(
    Guid Id,
    Guid FlashcardId,
    DateTime ReviewedAt,
    FlashcardRating Rating,
    FlashcardState StateBefore,
    int IntervalDaysBefore,
    int IntervalDaysAfter,
    int EaseFactorAfter
);
