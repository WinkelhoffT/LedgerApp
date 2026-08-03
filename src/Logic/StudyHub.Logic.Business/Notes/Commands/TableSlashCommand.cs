namespace StudyHub.Logic.Business.Notes.Commands;

public sealed class TableSlashCommand : ISlashCommand
{
    private const string Header = "| Column | Column |";
    private const string Separator = "|--------|--------|";
    private const string Row = "|        |        |";

    public string Id => "table";

    public string Title => "Table";

    public string Icon => "▦";

    public SlashCommandCategory Category => SlashCommandCategory.Code;

    public IReadOnlyList<string> Keywords { get; } = ["table", "grid", "rows"];

    public SlashCommandInsertion Apply(string precedingText)
    {
        var text = $"{Header}\n{Separator}\n{Row}";

        // First empty cell: just past the leading "|" of the data row.
        var rowStart = Header.Length + 1 + Separator.Length + 1;
        var cursorOffset = rowStart + 1;

        return new SlashCommandInsertion(text, cursorOffset);
    }
}
