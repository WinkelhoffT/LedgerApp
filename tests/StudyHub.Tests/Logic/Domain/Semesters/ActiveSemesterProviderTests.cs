using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Tests.Logic.Domain.Semesters;

public class ActiveSemesterProviderTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly ActiveSemesterProvider _sut = new();

    [Fact]
    public void GetActive_WithNoSemesters_ReturnsNull()
    {
        var result = _sut.GetActive([], Today);

        Assert.Null(result);
    }

    [Fact]
    public void GetActive_WithSemesterCoveringToday_ReturnsIt()
    {
        var semester = CreateSemester(Today.AddDays(-10), Today.AddDays(10), isArchived: false);

        var result = _sut.GetActive([semester], Today);

        Assert.Equal(semester.Id, result?.Id);
    }

    [Fact]
    public void GetActive_WithOnlyArchivedSemesterCoveringToday_ReturnsNull()
    {
        var semester = CreateSemester(Today.AddDays(-10), Today.AddDays(10), isArchived: true);

        var result = _sut.GetActive([semester], Today);

        Assert.Null(result);
    }

    [Fact]
    public void GetActive_WithOnlyPastAndFutureSemesters_ReturnsNull()
    {
        var past = CreateSemester(Today.AddDays(-30), Today.AddDays(-10), isArchived: false);
        var future = CreateSemester(Today.AddDays(10), Today.AddDays(30), isArchived: false);

        var result = _sut.GetActive([past, future], Today);

        Assert.Null(result);
    }

    [Fact]
    public void GetActive_WithOverlappingActiveSemesters_ReturnsMostRecentlyStarted()
    {
        var older = CreateSemester(Today.AddDays(-20), Today.AddDays(20), isArchived: false);
        var newer = CreateSemester(Today.AddDays(-5), Today.AddDays(35), isArchived: false);

        var result = _sut.GetActive([older, newer], Today);

        Assert.Equal(newer.Id, result?.Id);
    }

    private static Semester CreateSemester(DateOnly startDate, DateOnly endDate, bool isArchived)
    {
        var semester = Semester.Create("Winter 2025/26", startDate, endDate);
        if (isArchived)
        {
            semester.Archive();
        }

        return semester;
    }
}
