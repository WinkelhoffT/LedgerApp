using Moq;
using StudyHub.Logic.Business.Dashboard;
using StudyHub.Logic.Business.Semesters;
using StudyHub.Logic.Domain.SemesterProgress;

namespace StudyHub.Tests.Logic.Business.Dashboard;

public class DashboardManagementTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly SemesterProgress DefaultProgress = new(TotalDays: 10, ElapsedDays: 5, RemainingDays: 5, PercentComplete: 50);

    private readonly Mock<ISemesterManagement> _semesterManagement = new();
    private readonly Mock<ISemesterProgressCalculator> _progressCalculator = new();
    private readonly DashboardManagement _sut;

    public DashboardManagementTests()
    {
        _sut = new DashboardManagement(_semesterManagement.Object, _progressCalculator.Object);

        _progressCalculator
            .Setup(c => c.Calculate(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .Returns(DefaultProgress);
    }

    [Fact]
    public async Task GetSemesterProgressAsync_WithNoSemesters_ReturnsEmptyState()
    {
        _semesterManagement.Setup(m => m.GetAllAsync(default)).ReturnsAsync([]);

        var result = await _sut.GetSemesterProgressAsync();

        Assert.False(result.HasActiveSemester);
        Assert.Null(result.SemesterId);
        _progressCalculator.Verify(
            c => c.Calculate(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()), Times.Never);
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

    [Fact]
    public async Task GetSemesterProgressAsync_WithActiveSemester_CallsCalculatorWithSemesterDateRange()
    {
        var semester = CreateSemester(Today.AddDays(-10), Today.AddDays(10), isArchived: false);
        _semesterManagement.Setup(m => m.GetAllAsync(default)).ReturnsAsync([semester]);

        await _sut.GetSemesterProgressAsync();

        _progressCalculator.Verify(
            c => c.Calculate(semester.StartDate, semester.EndDate, Today), Times.Once);
    }

    [Fact]
    public async Task GetSemesterProgressAsync_WithActiveSemester_MapsCalculatorResultIntoDto()
    {
        var semester = CreateSemester(Today.AddDays(-10), Today.AddDays(10), isArchived: false);
        _semesterManagement.Setup(m => m.GetAllAsync(default)).ReturnsAsync([semester]);

        var progress = new SemesterProgress(TotalDays: 21, ElapsedDays: 11, RemainingDays: 10, PercentComplete: 52.38);
        _progressCalculator
            .Setup(c => c.Calculate(semester.StartDate, semester.EndDate, Today))
            .Returns(progress);

        var result = await _sut.GetSemesterProgressAsync();

        Assert.Equal(semester.Name, result.SemesterName);
        Assert.Equal(semester.StartDate, result.StartDate);
        Assert.Equal(semester.EndDate, result.EndDate);
        Assert.Equal(progress.TotalDays, result.TotalDays);
        Assert.Equal(progress.ElapsedDays, result.ElapsedDays);
        Assert.Equal(progress.RemainingDays, result.RemainingDays);
        Assert.Equal(progress.PercentComplete, result.PercentComplete);
    }

    private static SemesterDto CreateSemester(DateOnly startDate, DateOnly endDate, bool isArchived) =>
        new(Guid.NewGuid(), "Winter 2026/27", startDate, endDate, isArchived, DateTime.UtcNow, DateTime.UtcNow);
}
