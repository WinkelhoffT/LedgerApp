namespace StudyHub.Shared.Flashcards;

public sealed record UpdateFlashcardRequest(Guid Id, FlashcardDto Card);
