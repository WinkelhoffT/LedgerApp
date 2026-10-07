using StudyHub.Shared.CalendarEvents;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Domain rules for creating and changing a <see cref="CalendarEvent"/>. An exam is all-day or has a
/// start time and a duration; a deadline has an optional due time and never a duration. Events are
/// immutable records, so every operation returns a new instance. Start times are kept to the whole
/// minute; seconds are dropped.
/// </summary>
public interface ICalendarEventLifecycle
{
    /// <exception cref="CalendarEventValidationException">Kind, title, owner, time or location breaks an event rule.</exception>
    CalendarEvent Create(
        CalendarEventKind kind,
        string title,
        Guid? courseId,
        Guid? semesterId,
        DateOnly date,
        TimeOnly? startTime,
        int? durationMinutes,
        string? location
    );

    /// <exception cref="CalendarEventValidationException">Kind, title, owner, time or location breaks an event rule.</exception>
    CalendarEvent Update(
        CalendarEvent calendarEvent,
        CalendarEventKind kind,
        string title,
        Guid? courseId,
        Guid? semesterId,
        DateOnly date,
        TimeOnly? startTime,
        int? durationMinutes,
        string? location
    );
}
