namespace StudyHub.Logic.Business.Notes.Commands;

public sealed class HeadingSlashCommand : ISlashCommand
{
    private readonly string _prefix;

    public HeadingSlashCommand(int level)
    {
        if (level is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Heading level must be between 1 and 3.");
        }

        Level = level;
        _prefix = new string('#', level);
        Id = $"heading-{level}";
        Title = $"Heading {level}";
        Icon = _prefix;
        Keywords = ["heading", "title", "header", $"h{level}"];
    }

    public int Level { get; }

    public string Id { get; }

    public string Title { get; }

    public string Icon { get; }

    public SlashCommandCategory Category => SlashCommandCategory.Text;

    public IReadOnlyList<string> Keywords { get; }

    public SlashCommandInsertion Apply(string precedingText)
    {
        var text = $"{_prefix} ";
        return new SlashCommandInsertion(text, text.Length);
    }
}
