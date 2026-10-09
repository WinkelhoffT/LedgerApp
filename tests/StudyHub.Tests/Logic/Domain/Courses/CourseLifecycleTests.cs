using StudyHub.Logic.Domain;
using StudyHub.Shared.Courses;

namespace StudyHub.Tests.Logic.Domain.Courses;

public class CourseLifecycleTests
{
    private static readonly CourseLifecycle CourseLifecycle = new();

    private static readonly Guid SemesterId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var course = CourseLifecycle.Create(
            "Algorithms",
            "Intro to algorithms",
            "#2563eb",
            SemesterId
        );

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
        Assert.Throws<CourseValidationException>(() =>
            CourseLifecycle.Create(name!, null, "#2563eb", SemesterId)
        );
    }

    [Fact]
    public void Create_WithNameExceedingMaxLength_ThrowsValidationException()
    {
        var name = new string('a', CreateCourseRequest.NameMaxLength + 1);

        Assert.Throws<CourseValidationException>(() =>
            CourseLifecycle.Create(name, null, "#2563eb", SemesterId)
        );
    }

    [Fact]
    public void Create_WithoutColor_ThrowsValidationException()
    {
        Assert.Throws<CourseValidationException>(() =>
            CourseLifecycle.Create("Algorithms", null, "", SemesterId)
        );
    }

    [Fact]
    public void Create_WithoutSemesterId_ThrowsValidationException()
    {
        Assert.Throws<CourseValidationException>(() =>
            CourseLifecycle.Create("Algorithms", null, "#2563eb", Guid.Empty)
        );
    }

    [Fact]
    public void Update_WhenNotArchived_UpdatesFields()
    {
        var course = CourseLifecycle.Create("Algorithms", null, "#2563eb", SemesterId);
        var otherSemesterId = Guid.NewGuid();

        course = CourseLifecycle.Update(
            course,
            "Data Structures",
            "Updated description",
            "#16a34a",
            otherSemesterId
        );

        Assert.Equal("Data Structures", course.Name);
        Assert.Equal("Updated description", course.Description);
        Assert.Equal("#16a34a", course.Color);
        Assert.Equal(otherSemesterId, course.SemesterId);
    }

    [Fact]
    public void Update_WithoutSemesterId_ThrowsValidationException()
    {
        var course = CourseLifecycle.Create("Algorithms", null, "#2563eb", SemesterId);

        Assert.Throws<CourseValidationException>(() =>
            CourseLifecycle.Update(course, "Data Structures", null, "#16a34a", Guid.Empty)
        );
    }

    [Fact]
    public void Update_WhenArchived_ThrowsCourseArchivedException()
    {
        var course = CourseLifecycle.Create("Algorithms", null, "#2563eb", SemesterId);
        course = CourseLifecycle.Archive(course);

        Assert.Throws<CourseArchivedException>(() =>
            CourseLifecycle.Update(course, "Data Structures", null, "#16a34a", SemesterId)
        );
    }

    [Fact]
    public void Archive_SetsIsArchivedTrue()
    {
        var course = CourseLifecycle.Create("Algorithms", null, "#2563eb", SemesterId);

        course = CourseLifecycle.Archive(course);

        Assert.True(course.IsArchived);
    }

    [Fact]
    public void Restore_AfterArchive_SetsIsArchivedFalse()
    {
        var course = CourseLifecycle.Create("Algorithms", null, "#2563eb", SemesterId);
        course = CourseLifecycle.Archive(course);

        course = CourseLifecycle.Restore(course);

        Assert.False(course.IsArchived);
    }
}
