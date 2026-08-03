namespace StudyHub.Logic.Business.Notes.Commands;

public sealed class CodeBlockSlashCommand : ISlashCommand
{
    public const string CommandId = "code-block";
    public const string DefaultLanguage = "csharp";

    public string Id => CommandId;

    public string Title => "Code Block";

    public string Icon => "</>";

    public SlashCommandCategory Category => SlashCommandCategory.Code;

    public IReadOnlyList<string> Keywords { get; } = ["code", "codeblock", "snippet", "fence"];

    public SlashCommandInsertion Apply(string precedingText)
    {
        var opening = $"```{DefaultLanguage}\n";
        var text = $"{opening}\n```";
        return new SlashCommandInsertion(text, opening.Length);
    }
}
