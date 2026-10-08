using StudyHub.Shared.CalendarEvents;

namespace StudyHub.Data.Contract;

public interface ICalendarEventRepository
{
    Task<CalendarEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Events from <paramref name="from"/> to <paramref name="to"/>, both days included, ordered by date and time (all-day first).</summary>
    Task<IReadOnlyList<CalendarEvent>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>The first <paramref name="count"/> events on or after <paramref name="from"/>, ordered by date and time (all-day first).</summary>
    Task<IReadOnlyList<CalendarEvent>> GetUpcomingAsync(DateOnly from, int count, CancellationToken cancellationToken = default);

    Task AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);

    void Update(CalendarEvent calendarEvent);

    void Remove(CalendarEvent calendarEvent);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
