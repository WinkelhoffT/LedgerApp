using Moq;
using StudyHub.Logic.Business.Notes;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Documents;
using StudyHub.Logic.Domain.Notes;
using StudyHub.Logic.Domain.Semesters;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Documents;
using StudyHub.Shared.Notes;
using StudyHub.Shared.Semesters;

namespace StudyHub.Tests.Logic.Business.Notes;

public class NoteOrchestratorTests
{
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid SemesterId = Guid.NewGuid();
    private static readonly Guid DocumentId = Guid.NewGuid();

    private readonly Mock<INoteRepository> _noteRepository = new();
    private readonly Mock<IDocumentRepository> _documentRepository = new();
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly Mock<ISemesterRepository> _semesterRepository = new();
    private readonly NoteOrchestrator _sut;

    public NoteOrchestratorTests()
    {
        _sut = new NoteOrchestrator(_noteRepository.Object, _documentRepository.Object, _courseRepository.Object, _semesterRepository.Object);

        _courseRepository.Setup(r => r.GetByIdAsync(CourseId, default))
            .ReturnsAsync(Course.Create("Algorithms", null, "#2563eb", Guid.NewGuid()));

        _semesterRepository.Setup(r => r.GetByIdAsync(SemesterId, default))
            .ReturnsAsync(Semester.Create("Winter 2025/26", new DateOnly(2025, 10, 1), new DateOnly(2026, 3, 31)));

        _documentRepository.Setup(r => r.GetByIdAsync(DocumentId, default))
            .ReturnsAsync(Document.Create("Notes.pdf", "application/pdf", [1, 2, 3], CourseId, null));

        _noteRepository.Setup(r => r.GetAttachedDocumentIdsAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((IReadOnlyList<Guid>)[]);
        _noteRepository.Setup(r => r.GetLinkedNoteIdsAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((IReadOnlyList<Guid>)[]);
        _noteRepository.Setup(r => r.GetIdsByTitlesAsync(It.IsAny<IReadOnlyCollection<string>>(), default))
            .ReturnsAsync(new Dictionary<string, Guid>());
        _noteRepository.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync((IReadOnlyList<Note>)[]);
    }

    [Fact]
    public async Task CreateAsync_WithValidCourseNote_AddsNoteAndReturnsDto()
    {
        var request = new CreateNoteRequest("Lecture 1", "Content", "tag1, tag2", CourseId, null);

        var result = await _sut.CreateAsync(request);

        Assert.Equal("Lecture 1", result.Title);
        Assert.Equal(CourseId, result.CourseId);
        Assert.Null(result.SemesterId);
        Assert.Equal(["tag1", "tag2"], result.Tags);
        _noteRepository.Verify(r => r.AddAsync(It.IsAny<Note>(), default), Times.Once);
        _noteRepository.Verify(r => r.SaveChangesAsync(default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateAsync_WithValidSemesterNote_AddsNoteAndReturnsDto()
    {
        var request = new CreateNoteRequest("Syllabus", "Content", null, null, SemesterId);

        var result = await _sut.CreateAsync(request);

        Assert.Equal(SemesterId, result.SemesterId);
        Assert.Null(result.CourseId);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateTitle_ThrowsDuplicateNoteTitleException()
    {
        _noteRepository.Setup(r => r.ExistsByTitleAsync("Lecture 1", null, default)).ReturnsAsync(true);
        var request = new CreateNoteRequest("Lecture 1", "Content", null, CourseId, null);

        await Assert.ThrowsAsync<DuplicateNoteTitleException>(() => _sut.CreateAsync(request));

        _noteRepository.Verify(r => r.AddAsync(It.IsAny<Note>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCourseNotFound_ThrowsCourseNotFoundException()
    {
        var unknownCourseId = Guid.NewGuid();
        _courseRepository.Setup(r => r.GetByIdAsync(unknownCourseId, default)).ReturnsAsync((Course?)null);
        var request = new CreateNoteRequest("Title", "Content", null, unknownCourseId, null);

        await Assert.ThrowsAsync<CourseNotFoundException>(() => _sut.CreateAsync(request));

        _noteRepository.Verify(r => r.AddAsync(It.IsAny<Note>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCourseArchived_ThrowsCourseArchivedException()
    {
        var archivedCourse = Course.Create("Algorithms", null, "#2563eb", Guid.NewGuid());
        archivedCourse.Archive();
        _courseRepository.Setup(r => r.GetByIdAsync(archivedCourse.Id, default)).ReturnsAsync(archivedCourse);
        var request = new CreateNoteRequest("Title", "Content", null, archivedCourse.Id, null);

        await Assert.ThrowsAsync<CourseArchivedException>(() => _sut.CreateAsync(request));

        _noteRepository.Verify(r => r.AddAsync(It.IsAny<Note>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSemesterArchived_ThrowsSemesterArchivedException()
    {
        var archivedSemester = Semester.Create("Summer 2026", new DateOnly(2026, 4, 1), new DateOnly(2026, 9, 30));
        archivedSemester.Archive();
        _semesterRepository.Setup(r => r.GetByIdAsync(archivedSemester.Id, default)).ReturnsAsync(archivedSemester);
        var request = new CreateNoteRequest("Title", "Content", null, null, archivedSemester.Id);

        await Assert.ThrowsAsync<SemesterArchivedException>(() => _sut.CreateAsync(request));

        _noteRepository.Verify(r => r.AddAsync(It.IsAny<Note>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithWikiLinkToExistingNote_ResolvesLinks()
    {
        var targetId = Guid.NewGuid();
        _noteRepository.Setup(r => r.GetIdsByTitlesAsync(
                It.Is<IReadOnlyCollection<string>>(titles => titles.Contains("Graph Theory")), default))
            .ReturnsAsync(new Dictionary<string, Guid> { ["Graph Theory"] = targetId });
        var request = new CreateNoteRequest("Title", "See [[Graph Theory]] for background.", null, CourseId, null);

        await _sut.CreateAsync(request);

        _noteRepository.Verify(r => r.ReplaceLinksAsync(
            It.IsAny<Guid>(),
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(targetId)),
            default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenExistingNoteHasForwardWikiLink_ResolvesThatNotesLinksToo()
    {
        var forwardReferencingNote = Note.Create("Referencing Note", "See [[Future Note]] for details.", null, CourseId, null);
        _noteRepository.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync((IReadOnlyList<Note>)[forwardReferencingNote]);

        var capturedNoteId = Guid.Empty;
        _noteRepository.Setup(r => r.AddAsync(It.IsAny<Note>(), default))
            .Callback<Note, CancellationToken>((note, _) => capturedNoteId = note.Id)
            .Returns(Task.CompletedTask);
        _noteRepository.Setup(r => r.GetIdsByTitlesAsync(
                It.Is<IReadOnlyCollection<string>>(titles => titles.Contains("Future Note")), default))
            .ReturnsAsync(() => new Dictionary<string, Guid> { ["Future Note"] = capturedNoteId });

        var request = new CreateNoteRequest("Future Note", "Content", null, CourseId, null);

        var created = await _sut.CreateAsync(request);

        _noteRepository.Verify(r => r.ReplaceLinksAsync(
            forwardReferencingNote.Id,
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(created.Id)),
            default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenArchived_ThrowsNoteArchivedException()
    {
        var note = Note.Create("Title", "Content", null, CourseId, null);
        note.Archive();
        _noteRepository.Setup(r => r.GetByIdAsync(note.Id, default)).ReturnsAsync(note);

        var request = new UpdateNoteRequest(note.Id, "New title", "New content", null, CourseId, null);

        await Assert.ThrowsAsync<NoteArchivedException>(() => _sut.UpdateAsync(request));
    }

    [Fact]
    public async Task GetByIdAsync_WhenNoteNotFound_ThrowsNoteNotFoundException()
    {
        var id = Guid.NewGuid();
        _noteRepository.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Note?)null);

        await Assert.ThrowsAsync<NoteNotFoundException>(() => _sut.GetByIdAsync(id));
    }

    [Fact]
    public async Task ArchiveAsync_SetsNoteArchivedAndSaves()
    {
        var note = Note.Create("Title", "Content", null, CourseId, null);
        _noteRepository.Setup(r => r.GetByIdAsync(note.Id, default)).ReturnsAsync(note);

        var result = await _sut.ArchiveAsync(note.Id);

        Assert.True(result.IsArchived);
        _noteRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_SetsNoteNotArchivedAndSaves()
    {
        var note = Note.Create("Title", "Content", null, CourseId, null);
        note.Archive();
        _noteRepository.Setup(r => r.GetByIdAsync(note.Id, default)).ReturnsAsync(note);

        var result = await _sut.RestoreAsync(note.Id);

        Assert.False(result.IsArchived);
        _noteRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task AttachDocumentAsync_WithKnownDocument_AddsAttachment()
    {
        var note = Note.Create("Title", "Content", null, CourseId, null);
        _noteRepository.Setup(r => r.GetByIdAsync(note.Id, default)).ReturnsAsync(note);

        await _sut.AttachDocumentAsync(note.Id, DocumentId);

        _noteRepository.Verify(r => r.AddAttachmentAsync(note.Id, DocumentId, default), Times.Once);
    }

    [Fact]
    public async Task AttachDocumentAsync_WithUnknownDocument_ThrowsDocumentNotFoundException()
    {
        var note = Note.Create("Title", "Content", null, CourseId, null);
        _noteRepository.Setup(r => r.GetByIdAsync(note.Id, default)).ReturnsAsync(note);
        var unknownDocumentId = Guid.NewGuid();
        _documentRepository.Setup(r => r.GetByIdAsync(unknownDocumentId, default)).ReturnsAsync((Document?)null);

        await Assert.ThrowsAsync<DocumentNotFoundException>(() => _sut.AttachDocumentAsync(note.Id, unknownDocumentId));

        _noteRepository.Verify(r => r.AddAttachmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task DetachDocumentAsync_RemovesAttachment()
    {
        var note = Note.Create("Title", "Content", null, CourseId, null);
        _noteRepository.Setup(r => r.GetByIdAsync(note.Id, default)).ReturnsAsync(note);

        await _sut.DetachDocumentAsync(note.Id, DocumentId);

        _noteRepository.Verify(r => r.RemoveAttachmentAsync(note.Id, DocumentId, default), Times.Once);
    }

    [Fact]
    public async Task GetBacklinksAsync_ReturnsBacklinkDtosOrderedByTitle()
    {
        var note = Note.Create("Target", "Content", null, CourseId, null);
        _noteRepository.Setup(r => r.GetByIdAsync(note.Id, default)).ReturnsAsync(note);

        var sourceA = Note.Create("Zeta", "See [[Target]]", null, CourseId, null);
        var sourceB = Note.Create("Alpha", "See [[Target]]", null, CourseId, null);

        _noteRepository.Setup(r => r.GetBacklinkNoteIdsAsync(note.Id, default))
            .ReturnsAsync((IReadOnlyList<Guid>)[sourceA.Id, sourceB.Id]);
        _noteRepository.Setup(r => r.GetByIdsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(sourceA.Id) && ids.Contains(sourceB.Id)), default))
            .ReturnsAsync((IReadOnlyList<Note>)[sourceA, sourceB]);

        var backlinks = await _sut.GetBacklinksAsync(note.Id);

        Assert.Equal(["Alpha", "Zeta"], backlinks.Select(b => b.Title));
    }
}
