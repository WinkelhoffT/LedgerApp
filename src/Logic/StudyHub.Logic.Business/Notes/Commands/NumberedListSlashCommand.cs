namespace StudyHub.Logic.Business.Notes.Commands;

public sealed class NumberedListSlashCommand : ISlashCommand
{
    public string Id => "numbered-list";

    public string Title => "Numbered List";

    public string Icon => "1.";

    public SlashCommandCategory Category => SlashCommandCategory.Lists;

    public IReadOnlyList<string> Keywords { get; } = ["numbered", "list", "ordered", "ol"];

    public SlashCommandInsertion Apply(string precedingText)
    {
        const string text = "1. ";
        return new SlashCommandInsertion(text, text.Length);
    }
}
