namespace StudyHub.Shared.Flashcards;

public sealed record FlashcardDeckDueDto(Guid DeckId, string Name, FlashcardStudyCountsDto Counts);
