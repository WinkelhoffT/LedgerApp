namespace StudyHub.Shared.CalendarEvents;

/// <summary>An exam or deadline on the Dashboard.</summary>
/// <param name="DaysUntil">Days from today in the calendar time zone; 0 for today.</param>
public sealed record UpcomingCalendarEventDto(CalendarEventDto Event, int DaysUntil)
{
    public const int MaxCount = 5;
}
