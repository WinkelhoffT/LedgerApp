using Moq;
using StudyHub.Data.Contract.Semesters;
using StudyHub.Logic.Business.Dashboard;
using StudyHub.Logic.Domain.Contract.SemesterProgress;
using StudyHub.Logic.Domain.Contract.Semesters;
using StudyHub.Shared.Domain.Semesters;

namespace StudyHub.Tests.Logic.Business.Dashboard;

public class DashboardOrchestratorTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly DateOnly StartDate = Today.AddDays(-10);
    private static readonly DateOnly EndDate = Today.AddDays(10);

    private readonly Mock<ISemesterRepository> _semesterRepository = new();
    private readonly Mock<IActiveSemesterProvider> _activeSemesterProvider = new();
    private readonly Mock<ISemesterProgressCalculator> _progressCalculator = new();
    private readonly DashboardOrchestrator _sut;

    public DashboardOrchestratorTests()
    {
        _sut = new DashboardOrchestrator(_semesterRepository.Object, _activeSemesterProvider.Object, _progressCalculator.Object);

        _semesterRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);
    }

    [Fact]
    public async Task GetSemesterProgressAsync_WhenProviderFindsNoActiveSemester_ReturnsEmptyState()
    {
        _activeSemesterProvider
            .Setup(p => p.GetActive(It.IsAny<IReadOnlyList<Semester>>(), It.IsAny<DateOnly>()))
            .Returns((Semester?)null);

        var result = await _sut.GetSemesterProgressAsync();

        Assert.False(result.HasActiveSemester);
        Assert.Null(result.SemesterId);
        _progressCalculator.Verify(
            c => c.Calculate(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public async Task GetSemesterProgressAsync_WhenProviderFindsActiveSemester_MapsCalculatorResultIntoDto()
    {
        var semester = Semester.Create("Winter 2025/26", StartDate, EndDate);
        _activeSemesterProvider
            .Setup(p => p.GetActive(It.IsAny<IReadOnlyList<Semester>>(), It.IsAny<DateOnly>()))
            .Returns(semester);

        var progress = new SemesterProgress(TotalDays: 21, ElapsedDays: 11, RemainingDays: 10, PercentComplete: 52.38);
        _progressCalculator
            .Setup(c => c.Calculate(semester.StartDate, semester.EndDate, It.IsAny<DateOnly>()))
            .Returns(progress);

        var result = await _sut.GetSemesterProgressAsync();

        Assert.True(result.HasActiveSemester);
        Assert.Equal(semester.Id, result.SemesterId);
        Assert.Equal(semester.Name, result.SemesterName);
        Assert.Equal(semester.StartDate, result.StartDate);
        Assert.Equal(semester.EndDate, result.EndDate);
        Assert.Equal(progress.TotalDays, result.TotalDays);
        Assert.Equal(progress.ElapsedDays, result.ElapsedDays);
        Assert.Equal(progress.RemainingDays, result.RemainingDays);
        Assert.Equal(progress.PercentComplete, result.PercentComplete);
    }

    [Fact]
    public async Task GetSemesterProgressAsync_PassesRepositorySemestersToProvider()
    {
        var semester = Semester.Create("Winter 2025/26", StartDate, EndDate);
        var semesters = new List<Semester> { semester };
        _semesterRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync(semesters);
        _activeSemesterProvider
            .Setup(p => p.GetActive(semesters, It.IsAny<DateOnly>()))
            .Returns(semester);
        _progressCalculator
            .Setup(c => c.Calculate(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .Returns(new SemesterProgress(TotalDays: 21, ElapsedDays: 11, RemainingDays: 10, PercentComplete: 52.38));

        await _sut.GetSemesterProgressAsync();

        _activeSemesterProvider.Verify(p => p.GetActive(semesters, It.IsAny<DateOnly>()), Times.Once);
    }
}
