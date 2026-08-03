namespace StudyHub.Logic.Business.Notes.Commands;

public sealed class QuoteSlashCommand : ISlashCommand
{
    public string Id => "quote";

    public string Title => "Quote";

    public string Icon => "❝";

    public SlashCommandCategory Category => SlashCommandCategory.Text;

    public IReadOnlyList<string> Keywords { get; } = ["quote", "blockquote", "citation"];

    public SlashCommandInsertion Apply(string precedingText)
    {
        const string text = "> ";
        return new SlashCommandInsertion(text, text.Length);
    }
}
