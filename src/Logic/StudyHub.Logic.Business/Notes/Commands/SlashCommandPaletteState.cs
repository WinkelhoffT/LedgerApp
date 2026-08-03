namespace StudyHub.Logic.Business.Notes.Commands;

/// <summary>
/// Drives the slash command palette: search results and the currently highlighted item.
/// One instance belongs to a single open palette; the UI owns positioning and rendering only.
/// </summary>
public sealed class SlashCommandPaletteState(ISlashCommandRegistry registry)
{
    public bool IsOpen { get; private set; }

    public string Query { get; private set; } = string.Empty;

    public int SelectedIndex { get; private set; }

    public IReadOnlyList<ISlashCommand> Results { get; private set; } = [];

    public void Open(string query)
    {
        IsOpen = true;
        SetQuery(query);
    }

    public void SetQuery(string query)
    {
        Query = query;
        Results = registry.Search(query);
        SelectedIndex = Results.Count == 0 ? 0 : Math.Clamp(SelectedIndex, 0, Results.Count - 1);
    }

    public void MoveSelection(int delta)
    {
        if (Results.Count == 0)
        {
            return;
        }

        SelectedIndex = ((SelectedIndex + delta) % Results.Count + Results.Count) % Results.Count;
    }

    public void SetSelectedIndex(int index)
    {
        if (Results.Count == 0)
        {
            return;
        }

        SelectedIndex = Math.Clamp(index, 0, Results.Count - 1);
    }

    public ISlashCommand? GetSelected() => Results.Count > 0 ? Results[SelectedIndex] : null;

    public void Close()
    {
        IsOpen = false;
        Query = string.Empty;
        Results = [];
        SelectedIndex = 0;
    }
}
