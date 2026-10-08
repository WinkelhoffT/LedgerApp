namespace StudyHub.Shared.Flashcards;

/// <summary>How long until the card comes back if it is answered with <see cref="Rating"/>.</summary>
public sealed record FlashcardIntervalPreviewDto(FlashcardRating Rating, TimeSpan Interval);
