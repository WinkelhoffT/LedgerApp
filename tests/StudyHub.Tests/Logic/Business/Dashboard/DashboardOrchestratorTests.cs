using Microsoft.Extensions.Options;
using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Logic.Integration.Anki;
using StudyHub.Shared.Anki;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Dashboard;
using StudyHub.Shared.Semesters;

namespace StudyHub.Tests.Logic.Business.Dashboard;

public class DashboardOrchestratorTests
{
    private static readonly SemesterLifecycle SemesterLifecycle = new();

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly DateOnly StartDate = Today.AddDays(-10);
    private static readonly DateOnly EndDate = Today.AddDays(10);

    private readonly Mock<ISemesterRepository> _semesterRepository = new();
    private readonly Mock<IActiveSemesterProvider> _activeSemesterProvider = new();
    private readonly Mock<ISemesterProgressCalculator> _progressCalculator = new();
    private readonly Mock<IAnkiConnectAccessor> _ankiConnectAccessor = new();
    private readonly Mock<IAnkiDueCardsCalculator> _ankiDueCardsCalculator = new();
    private readonly AnkiConnectOptions _ankiConnectOptions = new() { Enabled = true };
    private readonly DashboardOrchestrator _sut;

    public DashboardOrchestratorTests()
    {
        _sut = new DashboardOrchestrator(
            _semesterRepository.Object,
            _activeSemesterProvider.Object,
            _progressCalculator.Object,
            _ankiConnectAccessor.Object,
            _ankiDueCardsCalculator.Object,
            Options.Create(_ankiConnectOptions));

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
        var semester = SemesterLifecycle.Create("Winter 2025/26", StartDate, EndDate);
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
        var semester = SemesterLifecycle.Create("Winter 2025/26", StartDate, EndDate);
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

    [Fact]
    public async Task GetAnkiStudyStatusAsync_WhenDisabled_ReturnsDisabledWithoutCallingAnki()
    {
        _ankiConnectOptions.Enabled = false;

        var result = await _sut.GetAnkiStudyStatusAsync();

        Assert.Equal(AnkiConnectionStatus.Disabled, result.Status);
        Assert.False(result.HasCardsToStudy);
        _ankiConnectAccessor.Verify(a => a.GetDeckCountsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAnkiStudyStatusAsync_WhenAnkiIsUnreachable_ReturnsUnavailableWithZeroCounts()
    {
        _ankiConnectAccessor
            .Setup(a => a.GetDeckCountsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AnkiConnectUnavailableException("Connection refused"));

        var result = await _sut.GetAnkiStudyStatusAsync();

        Assert.Equal(AnkiConnectionStatus.Unavailable, result.Status);
        Assert.False(result.HasCardsToStudy);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Decks);
    }

    [Fact]
    public async Task GetAnkiStudyStatusAsync_WhenConnected_MapsCalculatorResultIntoDto()
    {
        IReadOnlyList<AnkiDeckCountsDto> deckCounts =
        [
            new("Informatik", NewCount: 10, LearnCount: 2, ReviewCount: 30),
            new("Informatik::Algorithmen", NewCount: 4, LearnCount: 1, ReviewCount: 10),
        ];
        _ankiConnectAccessor.Setup(a => a.GetDeckCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(deckCounts);

        var dueCards = new AnkiDueCards(
            NewCount: 10, LearnCount: 2, ReviewCount: 30, HasCardsToStudy: true, TopLevelDecks: [deckCounts[0]]);
        _ankiDueCardsCalculator.Setup(c => c.Calculate(deckCounts)).Returns(dueCards);

        var result = await _sut.GetAnkiStudyStatusAsync();

        Assert.Equal(AnkiConnectionStatus.Connected, result.Status);
        Assert.True(result.HasCardsToStudy);
        Assert.Equal(10, result.NewCount);
        Assert.Equal(2, result.LearnCount);
        Assert.Equal(30, result.ReviewCount);
        Assert.Equal(42, result.TotalCount);
        Assert.Equal([deckCounts[0]], result.Decks);
    }
}
