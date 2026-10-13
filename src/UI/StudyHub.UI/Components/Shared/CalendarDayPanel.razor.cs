using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Calendar;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.StudySessions;

namespace StudyHub.UI.Components.Shared;

/// <summary>The selected day with its exams, deadlines and sessions next to the calendar.</summary>
public partial class CalendarDayPanel
{
    [Parameter, EditorRequired]
    public DateOnly Date { get; set; }

    [Parameter, EditorRequired]
    public DateOnly Today { get; set; }

    [Parameter, EditorRequired]
    public IReadOnlyList<CalendarSessionDto> Sessions { get; set; } = [];

    [Parameter, EditorRequired]
    public IReadOnlyList<CalendarEventEntryDto> Events { get; set; } = [];

    [Parameter]
    public EventCallback<StudySessionDto> OnSessionSelected { get; set; }

    /// <summary>A session was marked as done from its card.</summary>
    [Parameter]
    public EventCallback<StudySessionDto> OnSessionCompleted { get; set; }

    [Parameter]
    public EventCallback<CalendarEventDto> OnEventSelected { get; set; }

    [Parameter]
    public EventCallback OnAddSession { get; set; }

    [Parameter]
    public EventCallback OnAddEvent { get; set; }
}
