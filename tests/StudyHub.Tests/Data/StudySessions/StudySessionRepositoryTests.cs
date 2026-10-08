using Microsoft.EntityFrameworkCore;
using StudyHub.Data;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Tests.Data.StudySessions;

public class StudySessionRepositoryTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static StudySession Session(string title, DateOnly date, TimeOnly startTime) =>
        new(Guid.NewGuid(), title, null, null, date, startTime, 60, null, DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public async Task GetByDateRangeAsync_IncludesBothBoundaryDaysOrderedByDateAndStart()
    {
        await using var dbContext = CreateDbContext();
        var repository = new StudySessionRepository(dbContext);
        await repository.AddAsync(Session("Before", new DateOnly(2026, 10, 4), new TimeOnly(9, 0)));
        await repository.AddAsync(Session("Last day", new DateOnly(2026, 10, 11), new TimeOnly(8, 0)));
        await repository.AddAsync(Session("First day late", new DateOnly(2026, 10, 5), new TimeOnly(14, 0)));
        await repository.AddAsync(Session("First day early", new DateOnly(2026, 10, 5), new TimeOnly(9, 0)));
        await repository.AddAsync(Session("After", new DateOnly(2026, 10, 12), new TimeOnly(9, 0)));
        await repository.SaveChangesAsync();

        var sessions = await repository.GetByDateRangeAsync(new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 11));

        Assert.Equal(["First day early", "First day late", "Last day"], sessions.Select(s => s.Title));
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        await using var dbContext = CreateDbContext();
        var repository = new StudySessionRepository(dbContext);
        var session = Session("Graph review", new DateOnly(2026, 10, 8), new TimeOnly(9, 0));
        await repository.AddAsync(session);
        await repository.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        repository.Update(session with { DurationMinutes = 90 });
        await repository.SaveChangesAsync();

        Assert.Equal(90, (await repository.GetByIdAsync(session.Id))!.DurationMinutes);
    }

    [Fact]
    public async Task Remove_DeletesTheSession()
    {
        await using var dbContext = CreateDbContext();
        var repository = new StudySessionRepository(dbContext);
        var session = Session("Graph review", new DateOnly(2026, 10, 8), new TimeOnly(9, 0));
        await repository.AddAsync(session);
        await repository.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        repository.Remove(session);
        await repository.SaveChangesAsync();

        Assert.Null(await repository.GetByIdAsync(session.Id));
    }
}
