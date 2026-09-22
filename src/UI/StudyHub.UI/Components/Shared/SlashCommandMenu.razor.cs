using Microsoft.AspNetCore.Components;

namespace StudyHub.UI.Components.Shared;

public partial class SlashCommandMenu
{
    [Parameter, EditorRequired]
    public IReadOnlyList<SlashCommand> Commands { get; set; } = [];

    [Parameter]
    public int SelectedIndex { get; set; }

    [Parameter]
    public double TopPx { get; set; }

    [Parameter]
    public double LeftPx { get; set; }

    [Parameter]
    public EventCallback<SlashCommand> OnSelect { get; set; }

    [Parameter]
    public EventCallback<int> OnHover { get; set; }
}
