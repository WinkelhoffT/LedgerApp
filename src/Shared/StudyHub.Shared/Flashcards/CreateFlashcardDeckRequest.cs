namespace StudyHub.Shared.Flashcards;

public sealed record CreateFlashcardDeckRequest(string Name, Guid? CourseId, Guid? SemesterId, int NewCardsPerDay, int ReviewsPerDay);
