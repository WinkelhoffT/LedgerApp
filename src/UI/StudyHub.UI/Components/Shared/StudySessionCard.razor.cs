using Microsoft.AspNetCore.Components;
using StudyHub.Shared.StudySessions;

namespace StudyHub.UI.Components.Shared;

/// <summary>A session with its time, title, course or semester and location, as in the day panel and on the Dashboard.</summary>
public partial class StudySessionCard
{
    [Parameter, EditorRequired]
    public StudySessionDto Session { get; set; } = default!;

    /// <summary>Sessions before this date are shown dimmed.</summary>
    [Parameter]
    public DateOnly? Today { get; set; }

    /// <summary>Makes the card a button that reports the session when clicked.</summary>
    [Parameter]
    public EventCallback<StudySessionDto> OnSelected { get; set; }

    private string PastClass => Session.Date < Today ? "is-past" : string.Empty;
}
