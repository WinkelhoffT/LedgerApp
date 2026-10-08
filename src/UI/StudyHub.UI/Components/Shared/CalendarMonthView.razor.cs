using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Calendar;

namespace StudyHub.UI.Components.Shared;

/// <summary>
/// The month grid with up to three session chips per day. A click on a day or one of its chips
/// selects the day; the chips are too small to open a session directly.
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
        day.Sessions.Count > 0 ? "has-sessions" : null,
    }.Where(c => c is not null));
}
