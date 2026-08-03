namespace StudyHub.Logic.Business.Notes.Commands;

/// <summary>An active "/query" the caret is currently sitting inside of.</summary>
/// <param name="Start">Index of the "/" character within the block text.</param>
/// <param name="Query">The text typed after the "/", used to filter commands.</param>
public sealed record SlashTrigger(int Start, string Query);
