using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Calendar;

namespace StudyHub.UI.Components.Shared;

/// <summary>
/// The month grid with up to three chips per day, exams and deadlines before sessions. A click on a
/// day or one of its chips selects the day; the chips are too small to open an entry directly.
/// </summary>
public partial class CalendarMonthView
{
    private const int MaxChips = 3;

    [Parameter, EditorRequired]
    public CalendarMonthDto Month { get; set; } = default!;

    [Parameter]
    public DateOnly SelectedDate { get; set; }

    [Parameter]
    public EventCallback<DateOnly> OnDaySelected { get; set; }

    private string GetCellClass(CalendarDayDto day) => string.Join(' ', new[]
    {
        day.Date.Month != Month.Month ? "other" : null,
        day.Date == Month.Today ? "today" : null,
        day.Date == SelectedDate ? "sel" : null,
        day.Events.Count + day.Sessions.Count > 0 ? "has-entries" : null,
        day.Events.Count > 0 ? "has-events" : null,
    }.Where(c => c is not null));

    private static DayChips GetChips(CalendarDayDto day)
    {
        var events = day.Events.Take(MaxChips).ToList();
        var sessions = day.Sessions.Take(MaxChips - events.Count).ToList();

        return new DayChips(events, sessions, day.Events.Count + day.Sessions.Count - events.Count - sessions.Count);
    }

    private sealed record DayChips(IReadOnlyList<CalendarEventEntryDto> Events, IReadOnlyList<CalendarSessionDto> Sessions, int Hidden);
}
