using StudyHub.Logic.Domain;
using StudyHub.Shared.Documents;

namespace StudyHub.Tests.Logic.Domain.Documents;

public class DocumentLifecycleTests
{
    private static readonly DocumentLifecycle DocumentLifecycle = new();

    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid SemesterId = Guid.NewGuid();
    private static readonly byte[] Content = [1, 2, 3];

    [Fact]
    public void Create_WithCourseId_SetsProperties()
    {
        var document = DocumentLifecycle.Create("Notes.pdf", "application/pdf", Content, CourseId, semesterId: null);

        Assert.NotEqual(Guid.Empty, document.Id);
        Assert.Equal("Notes.pdf", document.FileName);
        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal(Content.Length, document.SizeBytes);
        Assert.Equal(Content, document.Content);
        Assert.Equal(CourseId, document.CourseId);
        Assert.Null(document.SemesterId);
        Assert.False(document.IsArchived);
    }

    [Fact]
    public void Create_WithSemesterId_SetsProperties()
    {
        var document = DocumentLifecycle.Create("Syllabus.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Content, courseId: null, SemesterId);

        Assert.Equal(SemesterId, document.SemesterId);
        Assert.Null(document.CourseId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutFileName_ThrowsValidationException(string? fileName)
    {
        Assert.Throws<DocumentValidationException>(() => DocumentLifecycle.Create(fileName!, "application/pdf", Content, CourseId, null));
    }

    [Fact]
    public void Create_WithFileNameExceedingMaxLength_ThrowsValidationException()
    {
        var fileName = new string('a', UploadDocumentRequest.FileNameMaxLength + 1);

        Assert.Throws<DocumentValidationException>(() => DocumentLifecycle.Create(fileName, "application/pdf", Content, CourseId, null));
    }

    [Fact]
    public void Create_WithEmptyContent_ThrowsValidationException()
    {
        Assert.Throws<DocumentValidationException>(() => DocumentLifecycle.Create("Notes.pdf", "application/pdf", [], CourseId, null));
    }

    [Fact]
    public void Create_WithBothCourseAndSemesterId_ThrowsValidationException()
    {
        Assert.Throws<DocumentValidationException>(() => DocumentLifecycle.Create("Notes.pdf", "application/pdf", Content, CourseId, SemesterId));
    }

    [Fact]
    public void Create_WithNeitherCourseNorSemesterId_ThrowsValidationException()
    {
        Assert.Throws<DocumentValidationException>(() => DocumentLifecycle.Create("Notes.pdf", "application/pdf", Content, courseId: null, semesterId: null));
    }

    [Fact]
    public void Archive_SetsIsArchivedTrue()
    {
        var document = DocumentLifecycle.Create("Notes.pdf", "application/pdf", Content, CourseId, null);

        document = DocumentLifecycle.Archive(document);

        Assert.True(document.IsArchived);
    }

    [Fact]
    public void Restore_AfterArchive_SetsIsArchivedFalse()
    {
        var document = DocumentLifecycle.Create("Notes.pdf", "application/pdf", Content, CourseId, null);
        document = DocumentLifecycle.Archive(document);

        document = DocumentLifecycle.Restore(document);

        Assert.False(document.IsArchived);
    }
}
