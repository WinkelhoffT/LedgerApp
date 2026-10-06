namespace StudyHub.Logic.Integration.Ai;

public sealed record FlashcardGenerationInput(string NoteTitle, string NoteContent, int CardCount, string? FocusHint);
