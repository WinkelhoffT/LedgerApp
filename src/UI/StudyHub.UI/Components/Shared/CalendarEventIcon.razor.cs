using Microsoft.AspNetCore.Components;
using StudyHub.Shared.CalendarEvents;

namespace StudyHub.UI.Components.Shared;

/// <summary>The kind's icon: a graduation cap for an exam, a flag for a deadline.</summary>
public partial class CalendarEventIcon
{
    [Parameter, EditorRequired]
    public CalendarEventKind Kind { get; set; }

    [Parameter]
    public int Size { get; set; } = 12;
}
