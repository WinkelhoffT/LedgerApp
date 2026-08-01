using Microsoft.EntityFrameworkCore;
using StudyHub.Data;
using StudyHub.Infrastructure.Semesters;
using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Tests.Infrastructure.Semesters;

public class SemesterRepositoryTests
{
    private static readonly DateOnly StartDate = new(2025, 10, 1);
    private static readonly DateOnly EndDate = new(2026, 3, 31);

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsSemester()
    {
        await using var dbContext = CreateDbContext();
        var repository = new SemesterRepository(dbContext);
        var semester = Semester.Create("Winter 2025/26", StartDate, EndDate);

        await repository.AddAsync(semester);
        await repository.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(semester.Id);
        Assert.NotNull(stored);
        Assert.Equal("Winter 2025/26", stored!.Name);
    }

    [Fact]
    public async Task ExistsByNameAsync_IsCaseInsensitive()
    {
        await using var dbContext = CreateDbContext();
        var repository = new SemesterRepository(dbContext);
        var semester = Semester.Create("Winter 2025/26", StartDate, EndDate);
        await repository.AddAsync(semester);
        await repository.SaveChangesAsync();

        var exists = await repository.ExistsByNameAsync("winter 2025/26");

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByNameAsync_ExcludingOwnId_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var repository = new SemesterRepository(dbContext);
        var semester = Semester.Create("Winter 2025/26", StartDate, EndDate);
        await repository.AddAsync(semester);
        await repository.SaveChangesAsync();

        var exists = await repository.ExistsByNameAsync("Winter 2025/26", excludingId: semester.Id);

        Assert.False(exists);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSemestersOrderedByStartDate()
    {
        await using var dbContext = CreateDbContext();
        var repository = new SemesterRepository(dbContext);
        await repository.AddAsync(Semester.Create("Summer 2026", new DateOnly(2026, 4, 1), new DateOnly(2026, 9, 30)));
        await repository.AddAsync(Semester.Create("Winter 2025/26", StartDate, EndDate));
        await repository.SaveChangesAsync();

        var all = await repository.GetAllAsync();

        Assert.Equal(["Winter 2025/26", "Summer 2026"], all.Select(s => s.Name));
    }
}
