namespace StudyHub.Shared.CalendarEvents;

public sealed class CalendarEventNotFoundException(Guid calendarEventId)
    : Exception($"Calendar event '{calendarEventId}' was not found.")
{
    public Guid CalendarEventId { get; } = calendarEventId;
}
