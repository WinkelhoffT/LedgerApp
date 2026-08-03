namespace StudyHub.Logic.Business.Notes.Commands;

public sealed class DividerSlashCommand : ISlashCommand
{
    public string Id => "divider";

    public string Title => "Divider";

    public string Icon => "―";

    public SlashCommandCategory Category => SlashCommandCategory.Text;

    public IReadOnlyList<string> Keywords { get; } = ["divider", "hr", "line", "separator", "rule"];

    public SlashCommandInsertion Apply(string precedingText)
    {
        // Two blank lines on each side, matching the spacing convention used throughout this note editor.
        var leading = precedingText.Length > 0 ? "\n\n\n" : string.Empty;
        var text = $"{leading}---\n\n\n";
        return new SlashCommandInsertion(text, text.Length);
    }
}
