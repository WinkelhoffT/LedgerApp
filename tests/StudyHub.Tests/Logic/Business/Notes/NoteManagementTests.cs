using Moq;
using StudyHub.Logic.Business.Courses;
using StudyHub.Logic.Business.Notes;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Notes;

namespace StudyHub.Tests.Logic.Business.Notes;

public class NoteManagementTests
{
    private static readonly Guid CourseId = Guid.NewGuid();

    private readonly Mock<INoteRepository> _noteRepository = new();
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly NoteManagement _sut;

    public NoteManagementTests()
    {
        _sut = new NoteManagement(_noteRepository.Object, _courseRepository.Object);

        _courseRepository.Setup(r => r.GetByIdAsync(CourseId, default))
            .ReturnsAsync(Course.Create("Algorithms", null, "#2563eb", Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_WithExistingCourse_AddsNoteAndReturnsDto()
    {
        var result = await _sut.CreateAsync(new CreateNoteRequest("SOLID Principles", "content", CourseId));

        Assert.Equal("SOLID Principles", result.Title);
        Assert.Equal(CourseId, result.CourseId);
        _noteRepository.Verify(r => r.AddAsync(It.IsAny<Note>(), default), Times.Once);
        _noteRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownCourse_ThrowsCourseNotFoundException()
    {
        var unknownCourseId = Guid.NewGuid();
        _courseRepository.Setup(r => r.GetByIdAsync(unknownCourseId, default)).ReturnsAsync((Course?)null);

        await Assert.ThrowsAsync<CourseNotFoundException>(
            () => _sut.CreateAsync(new CreateNoteRequest("Title", "content", unknownCourseId)));

        _noteRepository.Verify(r => r.AddAsync(It.IsAny<Note>(), default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenNoteNotFound_ThrowsNoteNotFoundException()
    {
        var id = Guid.NewGuid();
        _noteRepository.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Note?)null);

        await Assert.ThrowsAsync<NoteNotFoundException>(
            () => _sut.UpdateAsync(new UpdateNoteRequest(id, "New Title", "content", CourseId)));
    }

    [Fact]
    public async Task UpdateAsync_WhenNoteArchived_ThrowsNoteArchivedException()
    {
        var note = Note.Create("Title", "content", CourseId);
        note.Archive();
        _noteRepository.Setup(r => r.GetByIdAsync(note.Id, default)).ReturnsAsync(note);

        await Assert.ThrowsAsync<NoteArchivedException>(
            () => _sut.UpdateAsync(new UpdateNoteRequest(note.Id, "New Title", "content", CourseId)));
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownCourse_ThrowsCourseNotFoundException()
    {
        var note = Note.Create("Title", "content", CourseId);
        _noteRepository.Setup(r => r.GetByIdAsync(note.Id, default)).ReturnsAsync(note);
        var unknownCourseId = Guid.NewGuid();
        _courseRepository.Setup(r => r.GetByIdAsync(unknownCourseId, default)).ReturnsAsync((Course?)null);

        await Assert.ThrowsAsync<CourseNotFoundException>(
            () => _sut.UpdateAsync(new UpdateNoteRequest(note.Id, "New Title", "content", unknownCourseId)));
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesNoteAndSaves()
    {
        var note = Note.Create("Title", "content", CourseId);
        _noteRepository.Setup(r => r.GetByIdAsync(note.Id, default)).ReturnsAsync(note);

        var result = await _sut.UpdateAsync(new UpdateNoteRequest(note.Id, "New Title", "New content", CourseId));

        Assert.Equal("New Title", result.Title);
        Assert.Equal("New content", result.Content);
        _noteRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ArchiveAsync_SetsNoteArchivedAndSaves()
    {
        var note = Note.Create("Title", "content", CourseId);
        _noteRepository.Setup(r => r.GetByIdAsync(note.Id, default)).ReturnsAsync(note);

        var result = await _sut.ArchiveAsync(note.Id);

        Assert.True(result.IsArchived);
        _noteRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ArchiveAsync_WhenNoteNotFound_ThrowsNoteNotFoundException()
    {
        var id = Guid.NewGuid();
        _noteRepository.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Note?)null);

        await Assert.ThrowsAsync<NoteNotFoundException>(() => _sut.ArchiveAsync(id));
    }

    [Fact]
    public async Task GetByCourseIdAsync_MapsRepositoryResultsToDtos()
    {
        var note = Note.Create("Title", "content", CourseId);
        _noteRepository.Setup(r => r.GetByCourseIdAsync(CourseId, default)).ReturnsAsync([note]);

        var result = await _sut.GetByCourseIdAsync(CourseId);

        Assert.Single(result);
        Assert.Equal(note.Id, result[0].Id);
        Assert.Equal(CourseId, result[0].CourseId);
    }
}
