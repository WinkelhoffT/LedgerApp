using Microsoft.AspNetCore.Components;
using StudyHub.Shared.CalendarEvents;

namespace StudyHub.UI.Components.Shared;

/// <summary>An exam or deadline with its kind, time, title, course or semester and location, as in the day panel.</summary>
public partial class CalendarEventCard
{
    [Parameter, EditorRequired]
    public CalendarEventDto Event { get; set; } = default!;

    /// <summary>Events before this date are shown dimmed.</summary>
    [Parameter]
    public DateOnly? Today { get; set; }

    /// <summary>Makes the card a button that reports the event when clicked.</summary>
    [Parameter]
    public EventCallback<CalendarEventDto> OnSelected { get; set; }

    private string PastClass => Event.Date < Today ? "is-past" : string.Empty;
}
