namespace StudyHub.Logic.Business.Notes.Commands;

/// <summary>
/// A single entry in the note editor's slash command palette. Each command owns its own
/// markdown template and cursor placement so the UI never needs to know what a command inserts.
/// </summary>
public interface ISlashCommand
{
    string Id { get; }

    string Title { get; }

    string Icon { get; }

    SlashCommandCategory Category { get; }

    IReadOnlyList<string> Keywords { get; }

    /// <param name="precedingText">The block text before the "/" trigger, so a command can decide whether it needs leading spacing.</param>
    SlashCommandInsertion Apply(string precedingText);
}
