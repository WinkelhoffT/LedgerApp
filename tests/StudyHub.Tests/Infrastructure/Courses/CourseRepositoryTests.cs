using Microsoft.EntityFrameworkCore;
using StudyHub.Data;
using StudyHub.Infrastructure.Courses;
using StudyHub.Logic.Domain.Courses;

namespace StudyHub.Tests.Infrastructure.Courses;

public class CourseRepositoryTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsCourse()
    {
        await using var dbContext = CreateDbContext();
        var repository = new CourseRepository(dbContext);
        var course = Course.Create("Algorithms", "Description", "#2563eb");

        await repository.AddAsync(course);
        await repository.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(course.Id);
        Assert.NotNull(stored);
        Assert.Equal("Algorithms", stored!.Name);
    }

    [Fact]
    public async Task ExistsByNameAsync_IsCaseInsensitive()
    {
        await using var dbContext = CreateDbContext();
        var repository = new CourseRepository(dbContext);
        var course = Course.Create("Algorithms", null, "#2563eb");
        await repository.AddAsync(course);
        await repository.SaveChangesAsync();

        var exists = await repository.ExistsByNameAsync("algorithms");

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByNameAsync_ExcludingOwnId_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var repository = new CourseRepository(dbContext);
        var course = Course.Create("Algorithms", null, "#2563eb");
        await repository.AddAsync(course);
        await repository.SaveChangesAsync();

        var exists = await repository.ExistsByNameAsync("Algorithms", excludingId: course.Id);

        Assert.False(exists);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCoursesOrderedByName()
    {
        await using var dbContext = CreateDbContext();
        var repository = new CourseRepository(dbContext);
        await repository.AddAsync(Course.Create("Zoology", null, "#2563eb"));
        await repository.AddAsync(Course.Create("Algorithms", null, "#2563eb"));
        await repository.SaveChangesAsync();

        var all = await repository.GetAllAsync();

        Assert.Equal(["Algorithms", "Zoology"], all.Select(c => c.Name));
    }
}
