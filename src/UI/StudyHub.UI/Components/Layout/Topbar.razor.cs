using Microsoft.AspNetCore.Components;

namespace StudyHub.UI.Components.Layout;

public partial class Topbar : ComponentBase
{
    [Parameter]
    public bool IsDarkMode { get; set; }

    [Parameter]
    public EventCallback OnToggleDarkMode { get; set; }
}
