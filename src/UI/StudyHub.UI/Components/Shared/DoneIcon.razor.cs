using Microsoft.AspNetCore.Components;

namespace StudyHub.UI.Components.Shared;

/// <summary>The check mark of a session that is done.</summary>
public partial class DoneIcon
{
    [Parameter]
    public int Size { get; set; } = 12;
}
