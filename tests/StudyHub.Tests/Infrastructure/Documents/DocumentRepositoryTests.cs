using Microsoft.EntityFrameworkCore;
using StudyHub.Data;
using StudyHub.Infrastructure.Documents;
using StudyHub.Logic.Domain.Documents;

namespace StudyHub.Tests.Infrastructure.Documents;

public class DocumentRepositoryTests
{
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid SemesterId = Guid.NewGuid();
    private static readonly byte[] Content = [1, 2, 3];

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsDocument()
    {
        await using var dbContext = CreateDbContext();
        var repository = new DocumentRepository(dbContext);
        var document = Document.Create("Notes.pdf", "application/pdf", Content, CourseId, null);

        await repository.AddAsync(document);
        await repository.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(document.Id);
        Assert.NotNull(stored);
        Assert.Equal("Notes.pdf", stored!.FileName);
        Assert.Equal(Content, stored.Content);
    }

    [Fact]
    public async Task GetByCourseIdAsync_ReturnsOnlyDocumentsForThatCourse()
    {
        await using var dbContext = CreateDbContext();
        var repository = new DocumentRepository(dbContext);
        var otherCourseId = Guid.NewGuid();
        await repository.AddAsync(Document.Create("Notes.pdf", "application/pdf", Content, CourseId, null));
        await repository.AddAsync(Document.Create("Other.pdf", "application/pdf", Content, otherCourseId, null));
        await repository.SaveChangesAsync();

        var documents = await repository.GetByCourseIdAsync(CourseId);

        Assert.Equal(["Notes.pdf"], documents.Select(d => d.FileName));
    }

    [Fact]
    public async Task GetBySemesterIdAsync_ReturnsOnlyDocumentsForThatSemester()
    {
        await using var dbContext = CreateDbContext();
        var repository = new DocumentRepository(dbContext);
        await repository.AddAsync(Document.Create("Syllabus.pdf", "application/pdf", Content, null, SemesterId));
        await repository.SaveChangesAsync();

        var documents = await repository.GetBySemesterIdAsync(SemesterId);

        Assert.Equal(["Syllabus.pdf"], documents.Select(d => d.FileName));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDocumentsOrderedByFileName()
    {
        await using var dbContext = CreateDbContext();
        var repository = new DocumentRepository(dbContext);
        await repository.AddAsync(Document.Create("Zoology.pdf", "application/pdf", Content, CourseId, null));
        await repository.AddAsync(Document.Create("Algorithms.pdf", "application/pdf", Content, CourseId, null));
        await repository.SaveChangesAsync();

        var all = await repository.GetAllAsync();

        Assert.Equal(["Algorithms.pdf", "Zoology.pdf"], all.Select(d => d.FileName));
    }
}
