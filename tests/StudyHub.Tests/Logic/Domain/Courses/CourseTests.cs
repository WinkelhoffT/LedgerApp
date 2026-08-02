using StudyHub.Logic.Domain.Courses;

namespace StudyHub.Tests.Logic.Domain.Courses;

public class CourseTests
{
    private static readonly Guid SemesterId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var course = Course.Create("Algorithms", "Intro to algorithms", "#2563eb", SemesterId);

        Assert.NotEqual(Guid.Empty, course.Id);
        Assert.Equal("Algorithms", course.Name);
        Assert.Equal("Intro to algorithms", course.Description);
        Assert.Equal("#2563eb", course.Color);
        Assert.Equal(SemesterId, course.SemesterId);
        Assert.False(course.IsArchived);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutName_ThrowsValidationException(string? name)
    {
        Assert.Throws<CourseValidationException>(() => Course.Create(name!, null, "#2563eb", SemesterId));
    }

    [Fact]
    public void Create_WithNameExceedingMaxLength_ThrowsValidationException()
    {
        var name = new string('a', Course.NameMaxLength + 1);

        Assert.Throws<CourseValidationException>(() => Course.Create(name, null, "#2563eb", SemesterId));
    }

    [Fact]
    public void Create_WithoutColor_ThrowsValidationException()
    {
        Assert.Throws<CourseValidationException>(() => Course.Create("Algorithms", null, "", SemesterId));
    }

    [Fact]
    public void Create_WithoutSemesterId_ThrowsValidationException()
    {
        Assert.Throws<CourseValidationException>(() => Course.Create("Algorithms", null, "#2563eb", Guid.Empty));
    }

    [Fact]
    public void Update_WhenNotArchived_UpdatesFields()
    {
        var course = Course.Create("Algorithms", null, "#2563eb", SemesterId);
        var otherSemesterId = Guid.NewGuid();

        course.Update("Data Structures", "Updated description", "#16a34a", otherSemesterId);

        Assert.Equal("Data Structures", course.Name);
        Assert.Equal("Updated description", course.Description);
        Assert.Equal("#16a34a", course.Color);
        Assert.Equal(otherSemesterId, course.SemesterId);
    }

    [Fact]
    public void Update_WithoutSemesterId_ThrowsValidationException()
    {
        var course = Course.Create("Algorithms", null, "#2563eb", SemesterId);

        Assert.Throws<CourseValidationException>(() => course.Update("Data Structures", null, "#16a34a", Guid.Empty));
    }

    [Fact]
    public void Update_WhenArchived_ThrowsCourseArchivedException()
    {
        var course = Course.Create("Algorithms", null, "#2563eb", SemesterId);
        course.Archive();

        Assert.Throws<CourseArchivedException>(() => course.Update("Data Structures", null, "#16a34a", SemesterId));
    }

    [Fact]
    public void Archive_SetsIsArchivedTrue()
    {
        var course = Course.Create("Algorithms", null, "#2563eb", SemesterId);

        course.Archive();

        Assert.True(course.IsArchived);
    }

    [Fact]
    public void Restore_AfterArchive_SetsIsArchivedFalse()
    {
        var course = Course.Create("Algorithms", null, "#2563eb", SemesterId);
        course.Archive();

        course.Restore();

        Assert.False(course.IsArchived);
    }
}
