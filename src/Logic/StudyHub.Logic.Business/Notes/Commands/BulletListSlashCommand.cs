namespace StudyHub.Logic.Business.Notes.Commands;

public sealed class BulletListSlashCommand : ISlashCommand
{
    public string Id => "bullet-list";

    public string Title => "Bullet List";

    public string Icon => "•";

    public SlashCommandCategory Category => SlashCommandCategory.Lists;

    public IReadOnlyList<string> Keywords { get; } = ["bullet", "list", "unordered", "ul"];

    public SlashCommandInsertion Apply(string precedingText)
    {
        const string text = "- ";
        return new SlashCommandInsertion(text, text.Length);
    }
}
