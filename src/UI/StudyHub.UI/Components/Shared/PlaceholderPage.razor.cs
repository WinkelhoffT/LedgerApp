using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace StudyHub.UI.Components.Shared;

public partial class PlaceholderPage : ComponentBase
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string Icon { get; set; } = Icons.Material.Filled.Construction;

    [Parameter]
    public string Message { get; set; } = "Diese Seite ist noch nicht implementiert.";
}
