using Moq;
using StudyHub.Logic.Business.Courses;
using StudyHub.Logic.Business.Documents;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Documents;
using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Tests.Logic.Business.Documents;

public class DocumentManagementTests
{
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid SemesterId = Guid.NewGuid();
    private static readonly byte[] Content = [1, 2, 3];

    private readonly Mock<IDocumentRepository> _documentRepository = new();
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly Mock<ISemesterRepository> _semesterRepository = new();
    private readonly DocumentManagement _sut;

    public DocumentManagementTests()
    {
        _sut = new DocumentManagement(_documentRepository.Object, _courseRepository.Object, _semesterRepository.Object);

        _courseRepository.Setup(r => r.GetByIdAsync(CourseId, default))
            .ReturnsAsync(Course.Create("Algorithms", null, "#2563eb", Guid.NewGuid()));

        _semesterRepository.Setup(r => r.GetByIdAsync(SemesterId, default))
            .ReturnsAsync(Semester.Create("Winter 2025/26", new DateOnly(2025, 10, 1), new DateOnly(2026, 3, 31)));
    }

    [Fact]
    public async Task UploadAsync_WithValidCourseDocument_AddsDocumentAndReturnsDto()
    {
        var request = new UploadDocumentRequest("Notes.pdf", "application/pdf", Content, CourseId, null);

        var result = await _sut.UploadAsync(request);

        Assert.Equal("Notes.pdf", result.FileName);
        Assert.Equal(CourseId, result.CourseId);
        Assert.Null(result.SemesterId);
        Assert.Equal(Content.Length, result.SizeBytes);
        _documentRepository.Verify(r => r.AddAsync(It.IsAny<Document>(), default), Times.Once);
        _documentRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_WithValidSemesterDocument_AddsDocumentAndReturnsDto()
    {
        var request = new UploadDocumentRequest("Syllabus.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Content, null, SemesterId);

        var result = await _sut.UploadAsync(request);

        Assert.Equal(SemesterId, result.SemesterId);
        Assert.Null(result.CourseId);
    }

    [Fact]
    public async Task UploadAsync_WithUnsupportedContentType_ThrowsUnsupportedDocumentTypeException()
    {
        var request = new UploadDocumentRequest("Notes.txt", "text/plain", Content, CourseId, null);

        await Assert.ThrowsAsync<UnsupportedDocumentTypeException>(() => _sut.UploadAsync(request));

        _documentRepository.Verify(r => r.AddAsync(It.IsAny<Document>(), default), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WithContentExceedingMaxSize_ThrowsDocumentTooLargeException()
    {
        var oversizedContent = new byte[25 * 1024 * 1024 + 1];
        var request = new UploadDocumentRequest("Notes.pdf", "application/pdf", oversizedContent, CourseId, null);

        await Assert.ThrowsAsync<DocumentTooLargeException>(() => _sut.UploadAsync(request));

        _documentRepository.Verify(r => r.AddAsync(It.IsAny<Document>(), default), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WhenCourseNotFound_ThrowsCourseNotFoundException()
    {
        var unknownCourseId = Guid.NewGuid();
        _courseRepository.Setup(r => r.GetByIdAsync(unknownCourseId, default)).ReturnsAsync((Course?)null);
        var request = new UploadDocumentRequest("Notes.pdf", "application/pdf", Content, unknownCourseId, null);

        await Assert.ThrowsAsync<CourseNotFoundException>(() => _sut.UploadAsync(request));

        _documentRepository.Verify(r => r.AddAsync(It.IsAny<Document>(), default), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WhenCourseArchived_ThrowsCourseArchivedException()
    {
        var archivedCourse = Course.Create("Algorithms", null, "#2563eb", Guid.NewGuid());
        archivedCourse.Archive();
        _courseRepository.Setup(r => r.GetByIdAsync(archivedCourse.Id, default)).ReturnsAsync(archivedCourse);
        var request = new UploadDocumentRequest("Notes.pdf", "application/pdf", Content, archivedCourse.Id, null);

        await Assert.ThrowsAsync<CourseArchivedException>(() => _sut.UploadAsync(request));

        _documentRepository.Verify(r => r.AddAsync(It.IsAny<Document>(), default), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WhenSemesterArchived_ThrowsSemesterArchivedException()
    {
        var archivedSemester = Semester.Create("Summer 2026", new DateOnly(2026, 4, 1), new DateOnly(2026, 9, 30));
        archivedSemester.Archive();
        _semesterRepository.Setup(r => r.GetByIdAsync(archivedSemester.Id, default)).ReturnsAsync(archivedSemester);
        var request = new UploadDocumentRequest("Notes.pdf", "application/pdf", Content, null, archivedSemester.Id);

        await Assert.ThrowsAsync<SemesterArchivedException>(() => _sut.UploadAsync(request));

        _documentRepository.Verify(r => r.AddAsync(It.IsAny<Document>(), default), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDocumentNotFound_ThrowsDocumentNotFoundException()
    {
        var id = Guid.NewGuid();
        _documentRepository.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Document?)null);

        await Assert.ThrowsAsync<DocumentNotFoundException>(() => _sut.GetByIdAsync(id));
    }

    [Fact]
    public async Task DownloadAsync_ReturnsFileNameContentTypeAndContent()
    {
        var document = Document.Create("Notes.pdf", "application/pdf", Content, CourseId, null);
        _documentRepository.Setup(r => r.GetByIdAsync(document.Id, default)).ReturnsAsync(document);

        var result = await _sut.DownloadAsync(document.Id);

        Assert.Equal("Notes.pdf", result.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(Content, result.Content);
    }

    [Fact]
    public async Task ArchiveAsync_SetsDocumentArchivedAndSaves()
    {
        var document = Document.Create("Notes.pdf", "application/pdf", Content, CourseId, null);
        _documentRepository.Setup(r => r.GetByIdAsync(document.Id, default)).ReturnsAsync(document);

        var result = await _sut.ArchiveAsync(document.Id);

        Assert.True(result.IsArchived);
        _documentRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_SetsDocumentNotArchivedAndSaves()
    {
        var document = Document.Create("Notes.pdf", "application/pdf", Content, CourseId, null);
        document.Archive();
        _documentRepository.Setup(r => r.GetByIdAsync(document.Id, default)).ReturnsAsync(document);

        var result = await _sut.RestoreAsync(document.Id);

        Assert.False(result.IsArchived);
        _documentRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
