using StudyHub.Logic.Domain.Notes;

namespace StudyHub.Tests.Logic.Domain.Notes;

public class NoteTests
{
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid SemesterId = Guid.NewGuid();

    [Fact]
    public void Create_WithCourseId_SetsProperties()
    {
        var note = Note.Create("Lecture 1", "# Intro", "graphs, algorithms", CourseId, semesterId: null);

        Assert.NotEqual(Guid.Empty, note.Id);
        Assert.Equal("Lecture 1", note.Title);
        Assert.Equal("# Intro", note.Content);
        Assert.Equal("graphs, algorithms", note.Tags);
        Assert.Equal(CourseId, note.CourseId);
        Assert.Null(note.SemesterId);
        Assert.False(note.IsArchived);
    }

    [Fact]
    public void Create_WithSemesterId_SetsProperties()
    {
        var note = Note.Create("Syllabus overview", "Content", null, courseId: null, SemesterId);

        Assert.Equal(SemesterId, note.SemesterId);
        Assert.Null(note.CourseId);
        Assert.Null(note.Tags);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutTitle_ThrowsValidationException(string? title)
    {
        Assert.Throws<NoteValidationException>(() => Note.Create(title!, "Content", null, CourseId, null));
    }

    [Fact]
    public void Create_WithTitleExceedingMaxLength_ThrowsValidationException()
    {
        var title = new string('a', Note.TitleMaxLength + 1);

        Assert.Throws<NoteValidationException>(() => Note.Create(title, "Content", null, CourseId, null));
    }

    [Fact]
    public void Create_WithContentExceedingMaxLength_ThrowsValidationException()
    {
        var content = new string('a', Note.ContentMaxLength + 1);

        Assert.Throws<NoteValidationException>(() => Note.Create("Title", content, null, CourseId, null));
    }

    [Fact]
    public void Create_WithBothCourseAndSemesterId_ThrowsValidationException()
    {
        Assert.Throws<NoteValidationException>(() => Note.Create("Title", "Content", null, CourseId, SemesterId));
    }

    [Fact]
    public void Create_WithNeitherCourseNorSemesterId_ThrowsValidationException()
    {
        Assert.Throws<NoteValidationException>(() => Note.Create("Title", "Content", null, courseId: null, semesterId: null));
    }

    [Fact]
    public void Update_WhenNotArchived_UpdatesProperties()
    {
        var note = Note.Create("Title", "Content", null, CourseId, null);

        note.Update("New title", "New content", "tag1, tag2", null, SemesterId);

        Assert.Equal("New title", note.Title);
        Assert.Equal("New content", note.Content);
        Assert.Equal("tag1, tag2", note.Tags);
        Assert.Null(note.CourseId);
        Assert.Equal(SemesterId, note.SemesterId);
    }

    [Fact]
    public void Update_WhenArchived_ThrowsNoteArchivedException()
    {
        var note = Note.Create("Title", "Content", null, CourseId, null);
        note.Archive();

        Assert.Throws<NoteArchivedException>(() => note.Update("New title", "New content", null, CourseId, null));
    }

    [Fact]
    public void Archive_SetsIsArchivedTrue()
    {
        var note = Note.Create("Title", "Content", null, CourseId, null);

        note.Archive();

        Assert.True(note.IsArchived);
    }

    [Fact]
    public void Restore_AfterArchive_SetsIsArchivedFalse()
    {
        var note = Note.Create("Title", "Content", null, CourseId, null);
        note.Archive();

        note.Restore();

        Assert.False(note.IsArchived);
    }
}
