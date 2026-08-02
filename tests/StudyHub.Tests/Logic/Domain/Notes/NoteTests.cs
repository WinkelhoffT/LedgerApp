using StudyHub.Logic.Domain.Notes;

namespace StudyHub.Tests.Logic.Domain.Notes;

public class NoteTests
{
    private static readonly Guid CourseId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var note = Note.Create("SOLID Principles", "# SOLID\n\nSome content.", CourseId);

        Assert.NotEqual(Guid.Empty, note.Id);
        Assert.Equal("SOLID Principles", note.Title);
        Assert.Equal("# SOLID\n\nSome content.", note.Content);
        Assert.Equal(CourseId, note.CourseId);
        Assert.False(note.IsArchived);
    }

    [Fact]
    public void Create_WithEmptyContent_IsAllowed()
    {
        var note = Note.Create("Empty Note", "", CourseId);

        Assert.Equal(string.Empty, note.Content);
    }

    [Fact]
    public void Create_WithNullContent_DefaultsToEmptyString()
    {
        var note = Note.Create("Empty Note", null, CourseId);

        Assert.Equal(string.Empty, note.Content);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutTitle_ThrowsValidationException(string? title)
    {
        Assert.Throws<NoteValidationException>(() => Note.Create(title!, "content", CourseId));
    }

    [Fact]
    public void Create_WithTitleExceedingMaxLength_ThrowsValidationException()
    {
        var title = new string('a', Note.TitleMaxLength + 1);

        Assert.Throws<NoteValidationException>(() => Note.Create(title, "content", CourseId));
    }

    [Fact]
    public void Create_WithoutCourseId_ThrowsValidationException()
    {
        Assert.Throws<NoteValidationException>(() => Note.Create("Title", "content", Guid.Empty));
    }

    [Fact]
    public void Update_WhenNotArchived_UpdatesFields()
    {
        var note = Note.Create("Title", "content", CourseId);
        var otherCourseId = Guid.NewGuid();

        note.Update("New Title", "New content", otherCourseId);

        Assert.Equal("New Title", note.Title);
        Assert.Equal("New content", note.Content);
        Assert.Equal(otherCourseId, note.CourseId);
    }

    [Fact]
    public void Update_WithoutCourseId_ThrowsValidationException()
    {
        var note = Note.Create("Title", "content", CourseId);

        Assert.Throws<NoteValidationException>(() => note.Update("New Title", "content", Guid.Empty));
    }

    [Fact]
    public void Update_WhenArchived_ThrowsNoteArchivedException()
    {
        var note = Note.Create("Title", "content", CourseId);
        note.Archive();

        Assert.Throws<NoteArchivedException>(() => note.Update("New Title", "content", CourseId));
    }

    [Fact]
    public void Archive_SetsIsArchivedTrue()
    {
        var note = Note.Create("Title", "content", CourseId);

        note.Archive();

        Assert.True(note.IsArchived);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_DoesNotThrow()
    {
        var note = Note.Create("Title", "content", CourseId);
        note.Archive();

        var exception = Record.Exception(() => note.Archive());

        Assert.Null(exception);
    }
}
