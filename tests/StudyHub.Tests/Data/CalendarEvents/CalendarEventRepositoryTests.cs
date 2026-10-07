using Microsoft.EntityFrameworkCore;
using StudyHub.Data;
using StudyHub.Shared.CalendarEvents;

namespace StudyHub.Tests.Data.CalendarEvents;

public class CalendarEventRepositoryTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CalendarEvent Event(string title, DateOnly date, TimeOnly? startTime = null) =>
        new(
            Guid.NewGuid(),
            CalendarEventKind.Deadline,
            title,
            null,
            null,
            date,
            startTime,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

    private static async Task<CalendarEventRepository> CreateRepositoryAsync(
        ApplicationDbContext dbContext,
        params CalendarEvent[] events
    )
    {
        var repository = new CalendarEventRepository(dbContext);
        foreach (var calendarEvent in events)
        {
            await repository.AddAsync(calendarEvent);
        }

        await repository.SaveChangesAsync();
        return repository;
    }

    [Fact]
    public async Task GetByDateRangeAsync_IncludesBothBoundaryDaysWithAllDayEventsFirst()
    {
        await using var dbContext = CreateDbContext();
        var repository = await CreateRepositoryAsync(
            dbContext,
            Event("Before", new DateOnly(2026, 10, 4)),
            Event("Last day", new DateOnly(2026, 10, 11), new TimeOnly(23, 59)),
            Event("First day timed", new DateOnly(2026, 10, 5), new TimeOnly(10, 0)),
            Event("First day all-day", new DateOnly(2026, 10, 5)),
            Event("After", new DateOnly(2026, 10, 12))
        );

        var events = await repository.GetByDateRangeAsync(
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 11)
        );

        Assert.Equal(
            ["First day all-day", "First day timed", "Last day"],
            events.Select(e => e.Title)
        );
    }

    [Fact]
    public async Task GetUpcomingAsync_ReturnsTheFirstEventsFromTheGivenDayOn()
    {
        await using var dbContext = CreateDbContext();
        var repository = await CreateRepositoryAsync(
            dbContext,
            Event("Yesterday", new DateOnly(2026, 10, 7)),
            Event("Next week", new DateOnly(2026, 10, 15)),
            Event("Today late", new DateOnly(2026, 10, 8), new TimeOnly(23, 59)),
            Event("Today", new DateOnly(2026, 10, 8)),
            Event("Next month", new DateOnly(2026, 11, 2))
        );

        var events = await repository.GetUpcomingAsync(new DateOnly(2026, 10, 8), 3);

        Assert.Equal(["Today", "Today late", "Next week"], events.Select(e => e.Title));
    }

    [Fact]
    public async Task UpdateAndRemove_PersistChanges()
    {
        await using var dbContext = CreateDbContext();
        var calendarEvent = Event("Exam", new DateOnly(2026, 10, 8));
        var repository = await CreateRepositoryAsync(dbContext, calendarEvent);
        dbContext.ChangeTracker.Clear();

        repository.Update(calendarEvent with { Title = "Algorithms exam" });
        await repository.SaveChangesAsync();
        var updated = await repository.GetByIdAsync(calendarEvent.Id);
        dbContext.ChangeTracker.Clear();
        repository.Remove(calendarEvent);
        await repository.SaveChangesAsync();

        Assert.Equal("Algorithms exam", updated!.Title);
        Assert.Null(await repository.GetByIdAsync(calendarEvent.Id));
    }
}
