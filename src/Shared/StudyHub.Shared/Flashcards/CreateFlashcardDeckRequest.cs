namespace StudyHub.Shared.Flashcards;

public sealed record CreateFlashcardDeckRequest(string Name, Guid? CourseId, int NewCardsPerDay, int ReviewsPerDay);
