namespace StudyHub.Shared.Flashcards;

/// <summary>A row that was not imported; <see cref="LineNumber"/> is the 1-based line the row starts on.</summary>
public sealed record FlashcardImportFailureDto(int LineNumber, string Reason);
