using System.Globalization;
using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Calendar;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.StudySessions;
using StudyHub.UI.Calendar;

namespace StudyHub.UI.Components.Shared;

/// <summary>
/// The week as a time grid: one column per day, sessions and timed exams placed by start and
/// duration, overlapping ones side by side in the lanes the Api assigned. All-day exams and
/// deadlines sit in a row above the grid.
/// </summary>
public partial class CalendarWeekView
{
    private const int MinutesPerHour = 60;

    // Shorter entries are too low for two lines, so their block shows title and time in one line.
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
    public EventCallback<CalendarEventDto> OnEventSelected { get; set; }

    [Parameter]
    public EventCallback<CalendarSlot> OnSlotSelected { get; set; }

    // Only a timed exam has a duration; deadlines and all-day exams go to the all-day row.
    private static IEnumerable<CalendarEventEntryDto> GetAllDayEvents(CalendarDayDto day) =>
        day.Events.Where(e => e.Event.DurationMinutes is null);

    private static IEnumerable<CalendarEventEntryDto> GetTimedEvents(CalendarDayDto day) =>
        day.Events.Where(e => e.Event is { StartTime: not null, DurationMinutes: not null });

    private string GetDayClass(DateOnly date) => string.Join(' ', new[]
    {
        date == Week.Today ? "today" : null,
        date == SelectedDate ? "sel" : null,
    }.Where(c => c is not null));

    // Top and height in percent of the visible hours, left and width in percent of the day column.
    private string GetBlockStyle(TimeOnly startTime, int durationMinutes, int lane, int laneCount, string color)
    {
        var visibleMinutes = (double)(Week.EndHour - Week.StartHour) * MinutesPerHour;
        var startMinute = startTime.ToTimeSpan().TotalMinutes - Week.StartHour * MinutesPerHour;
        var laneWidth = 100.0 / laneCount;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"top:{startMinute / visibleMinutes * 100:0.###}%;" +
            $"height:{durationMinutes / visibleMinutes * 100:0.###}%;" +
            $"left:calc({lane * laneWidth:0.###}% + 2px);" +
            $"width:calc({laneWidth:0.###}% - 4px);" +
            $"--session-color:{color}");
    }
}
