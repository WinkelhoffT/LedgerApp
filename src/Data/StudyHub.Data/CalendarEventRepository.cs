using Microsoft.EntityFrameworkCore;
using StudyHub.Data.Contract;
using StudyHub.Shared.CalendarEvents;

namespace StudyHub.Data;

public sealed class CalendarEventRepository(ApplicationDbContext dbContext)
    : ICalendarEventRepository
{
    public Task<CalendarEvent?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        dbContext
            .CalendarEvents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CalendarEvent>> GetByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    ) =>
        await InDateOrder(
                dbContext.CalendarEvents.AsNoTracking().Where(e => e.Date >= from && e.Date <= to)
            )
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CalendarEvent>> GetUpcomingAsync(
        DateOnly from,
        int count,
        CancellationToken cancellationToken = default
    ) =>
        await InDateOrder(dbContext.CalendarEvents.AsNoTracking().Where(e => e.Date >= from))
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default
    ) => await dbContext.CalendarEvents.AddAsync(calendarEvent, cancellationToken);

    public void Update(CalendarEvent calendarEvent) =>
        dbContext.CalendarEvents.Update(calendarEvent);

    public void Remove(CalendarEvent calendarEvent) =>
        dbContext.CalendarEvents.Remove(calendarEvent);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    // All-day events (no start time) come before timed ones on the same day.
    private static IQueryable<CalendarEvent> InDateOrder(IQueryable<CalendarEvent> events) =>
        events
            .OrderBy(e => e.Date)
            .ThenBy(e => e.StartTime != null)
            .ThenBy(e => e.StartTime)
            .ThenBy(e => e.Title);
}
