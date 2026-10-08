using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Calendar;
using StudyHub.Shared.StudySessions;

namespace StudyHub.UI.Components.Shared;

/// <summary>The selected day with its session cards next to the calendar.</summary>
public partial class CalendarDayPanel
{
    [Parameter, EditorRequired]
    public DateOnly Date { get; set; }

    [Parameter, EditorRequired]
    public DateOnly Today { get; set; }

    [Parameter, EditorRequired]
    public IReadOnlyList<CalendarSessionDto> Sessions { get; set; } = [];

    [Parameter]
    public EventCallback<StudySessionDto> OnSessionSelected { get; set; }

    [Parameter]
    public EventCallback OnAddSession { get; set; }
}
