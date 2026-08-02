using Microsoft.EntityFrameworkCore;
using StudyHub.Data;
using StudyHub.Infrastructure.Notes;
using StudyHub.Logic.Domain.Notes;

namespace StudyHub.Tests.Infrastructure.Notes;

public class NoteRepositoryTests
{
    private static readonly Guid CourseId = Guid.NewGuid();

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsNote()
    {
        await using var dbContext = CreateDbContext();
        var repository = new NoteRepository(dbContext);
        var note = Note.Create("SOLID Principles", "content", CourseId);

        await repository.AddAsync(note);
        await repository.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(note.Id);
        Assert.NotNull(stored);
        Assert.Equal("SOLID Principles", stored!.Title);
        Assert.Equal(CourseId, stored.CourseId);
    }

    [Fact]
    public async Task GetByCourseIdAsync_ReturnsOnlyNotesForThatCourse()
    {
        await using var dbContext = CreateDbContext();
        var repository = new NoteRepository(dbContext);
        var otherCourseId = Guid.NewGuid();
        await repository.AddAsync(Note.Create("SOLID Principles", "content", CourseId));
        await repository.AddAsync(Note.Create("SQL Joins", "content", otherCourseId));
        await repository.SaveChangesAsync();

        var notes = await repository.GetByCourseIdAsync(CourseId);

        Assert.Equal(["SOLID Principles"], notes.Select(n => n.Title));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsNotesOrderedByMostRecentlyUpdated()
    {
        await using var dbContext = CreateDbContext();
        var repository = new NoteRepository(dbContext);
        var older = Note.Create("Older Note", "content", CourseId);
        await repository.AddAsync(older);
        await repository.SaveChangesAsync();

        var newer = Note.Create("Newer Note", "content", CourseId);
        await repository.AddAsync(newer);
        await repository.SaveChangesAsync();

        var all = await repository.GetAllAsync();

        Assert.Equal(["Newer Note", "Older Note"], all.Select(n => n.Title));
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var repository = new NoteRepository(dbContext);

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
