using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Semesters;
using StudyHub.Tests.Fakes;

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
    private readonly Mock<IFlashcardDeckRepository> _deckRepository = new();
    private readonly Mock<IFlashcardRepository> _flashcardRepository = new();
    private readonly DashboardOrchestrator _sut;

    public DashboardOrchestratorTests()
    {
        var studyDayProvider = new StudyDayProvider(
            new FlashcardStudyOptions(),
            new FixedTimeProvider(new DateTime(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc)));

        _sut = new DashboardOrchestrator(
            _semesterRepository.Object,
            _activeSemesterProvider.Object,
            _progressCalculator.Object,
            _deckRepository.Object,
            _flashcardRepository.Object,
            new StudyQueueProvider(),
            studyDayProvider);

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

    private static FlashcardDeck Deck(string name, bool isArchived = false) =>
        new(Guid.NewGuid(), name, null, null, 20, 200, isArchived, DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public async Task GetFlashcardsDueAsync_SumsActiveDecksAndListsTheDecksWithMostDueCards()
    {
        var decks = Enumerable.Range(1, 7).Select(i => Deck($"Deck {i}")).Append(Deck("Archiviert", isArchived: true)).ToList();
        _deckRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync(decks);
        _flashcardRepository
            .Setup(r => r.GetCardCountsAsync(new DateTime(2026, 10, 9, 2, 0, 0, DateTimeKind.Utc), null, default))
            .ReturnsAsync(decks.Select((deck, i) => new FlashcardDeckCardCounts(deck.Id, 50, i, 1, 2 * i)).ToList());
        _flashcardRepository
            .Setup(r => r.GetReviewCountsAsync(new DateTime(2026, 10, 8, 2, 0, 0, DateTimeKind.Utc), null, default))
            .ReturnsAsync([new FlashcardDeckReviewCounts(decks[6].Id, 20, 0)]);

        var result = await _sut.GetFlashcardsDueAsync();

        // Deck 7 used its 20 new cards today, so only its 1 learning and 12 review cards count;
        // it ties with Deck 5 at 13 due cards, and ties are ordered by name.
        Assert.Equal(new FlashcardStudyCountsDto(15, 7, 42), result.Total);
        Assert.Equal(["Deck 6", "Deck 5", "Deck 7", "Deck 4", "Deck 3"], result.Decks.Select(d => d.Name));
        Assert.Equal(new FlashcardStudyCountsDto(5, 1, 10), result.Decks[0].Counts);
    }

    [Fact]
    public async Task GetFlashcardsDueAsync_WithoutDecks_ReturnsEmptyTotals()
    {
        _deckRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);
        _flashcardRepository.Setup(r => r.GetCardCountsAsync(It.IsAny<DateTime>(), null, default)).ReturnsAsync([]);
        _flashcardRepository.Setup(r => r.GetReviewCountsAsync(It.IsAny<DateTime>(), null, default)).ReturnsAsync([]);

        var result = await _sut.GetFlashcardsDueAsync();

        Assert.Equal(FlashcardStudyCountsDto.Empty, result.Total);
        Assert.Empty(result.Decks);
    }
}
