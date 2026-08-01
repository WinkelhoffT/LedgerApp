using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Tests.Logic.Domain.Semesters;

public class SemesterTests
{
    private static readonly DateOnly StartDate = new(2025, 10, 1);
    private static readonly DateOnly EndDate = new(2026, 3, 31);

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var semester = Semester.Create("Winter 2025/26", StartDate, EndDate);

        Assert.NotEqual(Guid.Empty, semester.Id);
        Assert.Equal("Winter 2025/26", semester.Name);
        Assert.Equal(StartDate, semester.StartDate);
        Assert.Equal(EndDate, semester.EndDate);
        Assert.False(semester.IsArchived);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutName_ThrowsValidationException(string? name)
    {
        Assert.Throws<SemesterValidationException>(() => Semester.Create(name!, StartDate, EndDate));
    }

    [Fact]
    public void Create_WithNameExceedingMaxLength_ThrowsValidationException()
    {
        var name = new string('a', Semester.NameMaxLength + 1);

        Assert.Throws<SemesterValidationException>(() => Semester.Create(name, StartDate, EndDate));
    }

    [Fact]
    public void Create_WithEndDateBeforeStartDate_ThrowsValidationException()
    {
        Assert.Throws<SemesterValidationException>(() => Semester.Create("Winter 2025/26", EndDate, StartDate));
    }

    [Fact]
    public void Update_WhenNotArchived_UpdatesFields()
    {
        var semester = Semester.Create("Winter 2025/26", StartDate, EndDate);
        var newStart = new DateOnly(2026, 4, 1);
        var newEnd = new DateOnly(2026, 9, 30);

        semester.Update("Summer 2026", newStart, newEnd);

        Assert.Equal("Summer 2026", semester.Name);
        Assert.Equal(newStart, semester.StartDate);
        Assert.Equal(newEnd, semester.EndDate);
    }

    [Fact]
    public void Update_WhenArchived_ThrowsSemesterArchivedException()
    {
        var semester = Semester.Create("Winter 2025/26", StartDate, EndDate);
        semester.Archive();

        Assert.Throws<SemesterArchivedException>(() => semester.Update("Summer 2026", StartDate, EndDate));
    }

    [Fact]
    public void Archive_SetsIsArchivedTrue()
    {
        var semester = Semester.Create("Winter 2025/26", StartDate, EndDate);

        semester.Archive();

        Assert.True(semester.IsArchived);
    }

    [Fact]
    public void Restore_AfterArchive_SetsIsArchivedFalse()
    {
        var semester = Semester.Create("Winter 2025/26", StartDate, EndDate);
        semester.Archive();

        semester.Restore();

        Assert.False(semester.IsArchived);
    }
}
