using StudyHub.Shared.CalendarEvents;

namespace StudyHub.Logic.Integration.CalendarEvents;

/// <summary>Narrow HTTP access to StudyHub.Api's exam and deadline endpoints, covering the calendar's event dialog.</summary>
public interface ICalendarEventAccessor
{
    Task<CalendarEventDto> CreateAsync(CreateCalendarEventRequest request, CancellationToken cancellationToken = default);

    Task<CalendarEventDto> UpdateAsync(UpdateCalendarEventRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
