namespace StudyHub.Shared.Flashcards;

public sealed record ImportedFlashcardDeckDto(Guid DeckId, string Name, bool IsNew);
