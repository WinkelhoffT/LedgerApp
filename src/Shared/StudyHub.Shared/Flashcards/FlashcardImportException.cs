namespace StudyHub.Shared.Flashcards;

/// <summary>The whole file cannot be imported (size, encoding, row count, or no usable row).</summary>
public sealed class FlashcardImportException(string message) : Exception(message);
