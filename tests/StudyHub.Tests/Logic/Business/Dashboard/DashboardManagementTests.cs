using Moq;
using StudyHub.Logic.Business.Dashboard;
using StudyHub.Logic.Business.Semesters;

namespace StudyHub.Tests.Logic.Business.Dashboard;

public class DashboardManagementTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly Mock<ISemesterManagement> _semesterManagement = new();
    private readonly DashboardManagement _sut;

    public DashboardManagementTests()
    {
        _sut = new DashboardManagement(_semesterManagement.Object);
    }

    [Fact]
    public async Task GetSemesterProgressAsync_WithNoSemesters_ReturnsEmptyState()
    {
        _semesterManagement.Setup(m => m.GetAllAsync(default)).ReturnsAsync([]);

        var result = await _sut.GetSemesterProgressAsync();

        Assert.False(result.HasActiveSemester);
        Assert.Null(result.SemesterId);
    }

    [Fact]
    public async Task GetSemesterProgressAsync_WithSemesterCoveringToday_ReturnsItAsActive()
    {
        var semester = CreateSemester(Today.AddDays(-10), Today.AddDays(10), isArchived: false);
        _semesterManagement.Setup(m => m.GetAllAsync(default)).ReturnsAsync([semester]);

        var result = await _sut.GetSemesterProgressAsync();

        Assert.True(result.HasActiveSemester);
        Assert.Equal(semester.Id, result.SemesterId);
    }

    [Fact]
    public async Task GetSemesterProgressAsync_WithOnlyArchivedSemesterCoveringToday_ReturnsEmptyState()
    {
        var semester = CreateSemester(Today.AddDays(-10), Today.AddDays(10), isArchived: true);
        _semesterManagement.Setup(m => m.GetAllAsync(default)).ReturnsAsync([semester]);

        var result = await _sut.GetSemesterProgressAsync();

        Assert.False(result.HasActiveSemester);
    }

    [Fact]
    public async Task GetSemesterProgressAsync_WithOnlyPastAndFutureSemesters_ReturnsEmptyState()
    {
        var past = CreateSemester(Today.AddDays(-30), Today.AddDays(-10), isArchived: false);
        var future = CreateSemester(Today.AddDays(10), Today.AddDays(30), isArchived: false);
        _semesterManagement.Setup(m => m.GetAllAsync(default)).ReturnsAsync([past, future]);

        var result = await _sut.GetSemesterProgressAsync();

        Assert.False(result.HasActiveSemester);
    }

    [Fact]
    public async Task GetSemesterProgressAsync_WithOverlappingActiveSemesters_ReturnsMostRecentlyStarted()
    {
        var older = CreateSemester(Today.AddDays(-20), Today.AddDays(20), isArchived: false);
        var newer = CreateSemester(Today.AddDays(-5), Today.AddDays(35), isArchived: false);
        _semesterManagement.Setup(m => m.GetAllAsync(default)).ReturnsAsync([older, newer]);

        var result = await _sut.GetSemesterProgressAsync();

        Assert.Equal(newer.Id, result.SemesterId);
    }

    private static SemesterDto CreateSemester(DateOnly startDate, DateOnly endDate, bool isArchived) =>
        new(Guid.NewGuid(), "Winter 2026/27", startDate, endDate, isArchived, DateTime.UtcNow, DateTime.UtcNow);
}
