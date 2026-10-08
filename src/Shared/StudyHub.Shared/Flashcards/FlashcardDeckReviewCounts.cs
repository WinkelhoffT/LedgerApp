namespace StudyHub.Shared.Flashcards;

/// <summary>Per-deck answers given since the start of the study day, counted against the daily limits.</summary>
/// <param name="NewStudied">Answers to cards that were new.</param>
/// <param name="ReviewsDone">Answers to cards that were in review.</param>
public sealed record FlashcardDeckReviewCounts(Guid DeckId, int NewStudied, int ReviewsDone);
