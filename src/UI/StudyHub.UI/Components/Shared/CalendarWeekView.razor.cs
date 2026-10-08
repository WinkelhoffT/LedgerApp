using System.Globalization;
using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Calendar;
using StudyHub.Shared.StudySessions;
using StudyHub.UI.Calendar;

namespace StudyHub.UI.Components.Shared;

/// <summary>
/// The week as a time grid: one column per day, sessions placed by start and duration, and
/// overlapping sessions side by side in the lanes the Api assigned.
/// </summary>
public partial class CalendarWeekView
{
    private const int MinutesPerHour = 60;

    // Shorter sessions are too low for two lines, so their block shows title and time in one line.
    private const int CompactMinutes = 45;

    [Parameter, EditorRequired]
    public CalendarWeekDto Week { get; set; } = default!;

    [Parameter]
    public DateOnly SelectedDate { get; set; }

    [Parameter]
    public EventCallback<DateOnly> OnDaySelected { get; set; }

    [Parameter]
    public EventCallback<StudySessionDto> OnSessionSelected { get; set; }

    [Parameter]
    public EventCallback<CalendarSlot> OnSlotSelected { get; set; }

    private string GetDayClass(DateOnly date) => string.Join(' ', new[]
    {
        date == Week.Today ? "today" : null,
        date == SelectedDate ? "sel" : null,
    }.Where(c => c is not null));

    // Top and height in percent of the visible hours, left and width in percent of the day column.
    private string GetBlockStyle(CalendarSessionDto item)
    {
        var visibleMinutes = (double)(Week.EndHour - Week.StartHour) * MinutesPerHour;
        var startMinute = item.Session.StartTime.ToTimeSpan().TotalMinutes - Week.StartHour * MinutesPerHour;
        var laneWidth = 100.0 / item.LaneCount;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"top:{startMinute / visibleMinutes * 100:0.###}%;" +
            $"height:{item.Session.DurationMinutes / visibleMinutes * 100:0.###}%;" +
            $"left:calc({item.Lane * laneWidth:0.###}% + 2px);" +
            $"width:calc({laneWidth:0.###}% - 4px);" +
            $"--session-color:{CalendarFormatter.GetColor(item.Session)}");
    }
}
