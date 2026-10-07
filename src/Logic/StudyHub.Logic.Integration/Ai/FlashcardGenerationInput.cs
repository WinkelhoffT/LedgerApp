namespace StudyHub.Logic.Integration.Ai;

public sealed record FlashcardGenerationInput(string Model, string NoteTitle, string NoteContent, int CardCount, string? FocusHint);
