namespace StudyHub.Shared.Flashcards;

public sealed record AnswerFlashcardRequest(Guid CardId, FlashcardRating Rating);
