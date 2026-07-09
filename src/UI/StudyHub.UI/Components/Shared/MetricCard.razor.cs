using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace StudyHub.UI.Components.Shared;

public partial class MetricCard : ComponentBase
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public string Icon { get; set; } = Icons.Material.Filled.Info;

    [Parameter]
    public Color Color { get; set; } = Color.Default;
}
