using Microsoft.EntityFrameworkCore;
using StudyHub.Data;
using StudyHub.Infrastructure.Notes;
using StudyHub.Logic.Domain.Documents;
using StudyHub.Logic.Domain.Notes;

namespace StudyHub.Tests.Infrastructure.Notes;

public class NoteRepositoryTests
{
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid SemesterId = Guid.NewGuid();

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
        var note = Note.Create("Lecture 1", "Content", "tag1", CourseId, null);

        await repository.AddAsync(note);
        await repository.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(note.Id);
        Assert.NotNull(stored);
        Assert.Equal("Lecture 1", stored!.Title);
    }

    [Fact]
    public async Task GetByCourseIdAsync_ReturnsOnlyNotesForThatCourse()
    {
        await using var dbContext = CreateDbContext();
        var repository = new NoteRepository(dbContext);
        var otherCourseId = Guid.NewGuid();
        await repository.AddAsync(Note.Create("Note A", "Content", null, CourseId, null));
        await repository.AddAsync(Note.Create("Note B", "Content", null, otherCourseId, null));
        await repository.SaveChangesAsync();

        var notes = await repository.GetByCourseIdAsync(CourseId);

        Assert.Equal(["Note A"], notes.Select(n => n.Title));
    }

    [Fact]
    public async Task GetBySemesterIdAsync_ReturnsOnlyNotesForThatSemester()
    {
        await using var dbContext = CreateDbContext();
        var repository = new NoteRepository(dbContext);
        await repository.AddAsync(Note.Create("Syllabus", "Content", null, null, SemesterId));
        await repository.SaveChangesAsync();

        var notes = await repository.GetBySemesterIdAsync(SemesterId);

        Assert.Equal(["Syllabus"], notes.Select(n => n.Title));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsNotesOrderedByTitle()
    {
        await using var dbContext = CreateDbContext();
        var repository = new NoteRepository(dbContext);
        await repository.AddAsync(Note.Create("Zeta", "Content", null, CourseId, null));
        await repository.AddAsync(Note.Create("Alpha", "Content", null, CourseId, null));
        await repository.SaveChangesAsync();

        var all = await repository.GetAllAsync();

        Assert.Equal(["Alpha", "Zeta"], all.Select(n => n.Title));
    }

    [Fact]
    public async Task ExistsByTitleAsync_IsCaseInsensitiveAndExcludesGivenId()
    {
        await using var dbContext = CreateDbContext();
        var repository = new NoteRepository(dbContext);
        var note = Note.Create("Lecture 1", "Content", null, CourseId, null);
        await repository.AddAsync(note);
        await repository.SaveChangesAsync();

        Assert.True(await repository.ExistsByTitleAsync("lecture 1", excludingId: null));
        Assert.False(await repository.ExistsByTitleAsync("lecture 1", excludingId: note.Id));
        Assert.False(await repository.ExistsByTitleAsync("Unrelated", excludingId: null));
    }

    [Fact]
    public async Task AddAttachmentAsync_ThenRemoveAttachmentAsync_RoundTrips()
    {
        await using var dbContext = CreateDbContext();
        var repository = new NoteRepository(dbContext);
        var note = Note.Create("Lecture 1", "Content", null, CourseId, null);
        var document = Document.Create("Notes.pdf", "application/pdf", [1, 2, 3], CourseId, null);
        dbContext.Documents.Add(document);
        await repository.AddAsync(note);
        await repository.SaveChangesAsync();

        await repository.AddAttachmentAsync(note.Id, document.Id);
        await repository.SaveChangesAsync();

        Assert.Equal([document.Id], await repository.GetAttachedDocumentIdsAsync(note.Id));

        await repository.RemoveAttachmentAsync(note.Id, document.Id);
        await repository.SaveChangesAsync();

        Assert.Empty(await repository.GetAttachedDocumentIdsAsync(note.Id));
    }

    [Fact]
    public async Task ReplaceLinksAsync_UpdatesLinkedAndBacklinkNoteIds()
    {
        await using var dbContext = CreateDbContext();
        var repository = new NoteRepository(dbContext);
        var source = Note.Create("Source", "Content", null, CourseId, null);
        var target = Note.Create("Target", "Content", null, CourseId, null);
        await repository.AddAsync(source);
        await repository.AddAsync(target);
        await repository.SaveChangesAsync();

        await repository.ReplaceLinksAsync(source.Id, [target.Id]);
        await repository.SaveChangesAsync();

        Assert.Equal([target.Id], await repository.GetLinkedNoteIdsAsync(source.Id));
        Assert.Equal([source.Id], await repository.GetBacklinkNoteIdsAsync(target.Id));

        await repository.ReplaceLinksAsync(source.Id, []);
        await repository.SaveChangesAsync();

        Assert.Empty(await repository.GetLinkedNoteIdsAsync(source.Id));
        Assert.Empty(await repository.GetBacklinkNoteIdsAsync(target.Id));
    }

    [Fact]
    public async Task GetIdsByTitlesAsync_ResolvesCaseInsensitively()
    {
        await using var dbContext = CreateDbContext();
        var repository = new NoteRepository(dbContext);
        var note = Note.Create("Graph Theory", "Content", null, CourseId, null);
        await repository.AddAsync(note);
        await repository.SaveChangesAsync();

        var result = await repository.GetIdsByTitlesAsync(["graph theory", "Unknown Title"]);

        Assert.Single(result);
        Assert.Equal(note.Id, result["Graph Theory"]);
    }
}
