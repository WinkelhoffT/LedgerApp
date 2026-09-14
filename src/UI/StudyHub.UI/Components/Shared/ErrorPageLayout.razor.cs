using Microsoft.AspNetCore.Components;

namespace StudyHub.UI.Components.Shared;

public partial class ErrorPageLayout
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    [Parameter, EditorRequired]
    public string Message { get; set; } = "";

    [Parameter]
    public RenderFragment? IconContent { get; set; }

    [Parameter]
    public string ButtonText { get; set; } = "Zurück zum Dashboard";

    [Parameter]
    public string ButtonHref { get; set; } = "/";

    [Parameter]
    public bool ShowDetails { get; set; } = true;

    [Parameter]
    public RenderFragment? Details { get; set; }
}
