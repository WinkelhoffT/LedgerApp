namespace StudyHub.Logic.Business.Notes.Commands;

/// <summary>
/// The markdown text a command inserts in place of the "/query" trigger, and where the caret
/// should land afterwards, expressed as an offset from the start of <see cref="Text"/>.
/// </summary>
public sealed record SlashCommandInsertion(string Text, int CursorOffset);
