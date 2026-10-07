namespace StudyHub.Shared.Flashcards;

public sealed record FlashcardExportDto(string FileName, string ContentType, byte[] Content);
