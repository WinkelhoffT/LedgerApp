namespace StudyHub.Shared.Analytics;

/// <summary>A flashcard answer as the study time statistics read it.</summary>
/// <param name="CourseId">The course of the card's deck, if the deck belongs to one.</param>
public sealed record FlashcardReviewActivity(DateTime ReviewedAt, Guid? CourseId);
