namespace StudyHub.Shared.Flashcards;

public sealed record UpdateFlashcardDeckRequest(Guid Id, string Name, Guid? CourseId, int NewCardsPerDay, int ReviewsPerDay);
