using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Business.Flashcards;

public class FlashcardDeckOrchestratorTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TodayStart = new(2026, 10, 8, 2, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TomorrowStart = new(2026, 10, 9, 2, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IFlashcardDeckRepository> _deckRepository = new();
    private readonly Mock<IFlashcardRepository> _flashcardRepository = new();
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly FlashcardDeckOrchestrator _sut;

    public FlashcardDeckOrchestratorTests()
    {
        var timeProvider = new FixedTimeProvider(Now);
        _sut = new FlashcardDeckOrchestrator(
            _deckRepository.Object,
            _flashcardRepository.Object,
            _courseRepository.Object,
            new FlashcardDeckLifecycle(timeProvider),
            new StudyQueueProvider(),
            new StudyDayProvider(new FlashcardStudyOptions(), timeProvider));

        _flashcardRepository.Setup(r => r.GetCardCountsAsync(It.IsAny<DateTime>(), It.IsAny<Guid?>(), default)).ReturnsAsync([]);
        _flashcardRepository.Setup(r => r.GetReviewCountsAsync(It.IsAny<DateTime>(), It.IsAny<Guid?>(), default)).ReturnsAsync([]);
    }

    private FlashcardDeck SetupDeck(string name = "Algorithmen", bool isArchived = false, Guid? courseId = null)
    {
        var deck = new FlashcardDeck(Guid.NewGuid(), name, courseId, 20, 200, isArchived, Now, Now);
        _deckRepository.Setup(r => r.GetByIdAsync(deck.Id, default)).ReturnsAsync(deck);
        return deck;
    }

    private Course SetupCourse(bool isArchived = false)
    {
        var course = new Course(Guid.NewGuid(), "Algorithmen", null, "#2563eb", Guid.NewGuid(), isArchived, Now, Now);
        _courseRepository.Setup(r => r.GetByIdAsync(course.Id, default)).ReturnsAsync(course);
        return course;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsActiveDecksWithTodaysCountsAfterDailyLimits()
    {
        var deck = SetupDeck();
        var archived = SetupDeck("Alt", isArchived: true);
        _deckRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([deck, archived]);
        _flashcardRepository.Setup(r => r.GetCardCountsAsync(TomorrowStart, null, default))
            .ReturnsAsync([new FlashcardDeckCardCounts(deck.Id, 120, 40, 2, 7)]);
        _flashcardRepository.Setup(r => r.GetReviewCountsAsync(TodayStart, null, default))
            .ReturnsAsync([new FlashcardDeckReviewCounts(deck.Id, 15, 3)]);

        var result = await _sut.GetAllAsync(includeArchived: false);

        var dto = Assert.Single(result);
        Assert.Equal(120, dto.CardCount);
        Assert.Equal(new FlashcardStudyCountsDto(5, 2, 7), dto.DueCounts);
    }

    [Fact]
    public async Task GetAllAsync_WithArchived_IncludesArchivedDecksWithoutDueCards()
    {
        var archived = SetupDeck("Alt", isArchived: true);
        _deckRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([archived]);
        _flashcardRepository.Setup(r => r.GetCardCountsAsync(TomorrowStart, null, default))
            .ReturnsAsync([new FlashcardDeckCardCounts(archived.Id, 10, 10, 0, 0)]);

        var dto = Assert.Single(await _sut.GetAllAsync(includeArchived: true));

        Assert.Equal(10, dto.CardCount);
        Assert.Equal(FlashcardStudyCountsDto.Empty, dto.DueCounts);
    }

    [Fact]
    public async Task CreateAsync_WithCourse_SavesNormalizedDeck()
    {
        var course = SetupCourse();

        var result = await _sut.CreateAsync(new CreateFlashcardDeckRequest(" Algorithmen \n Graphen ", course.Id, 10, 100));

        Assert.Equal("Algorithmen Graphen", result.Name);
        Assert.Equal(course.Id, result.CourseId);
        Assert.Equal(0, result.CardCount);
        _deckRepository.Verify(r => r.AddAsync(It.Is<FlashcardDeck>(d => d.Name == "Algorithmen Graphen" && d.NewCardsPerDay == 10), default), Times.Once);
        _deckRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_Throws()
    {
        _deckRepository.Setup(r => r.ExistsByNameAsync("Algorithmen", null, default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<DuplicateFlashcardDeckNameException>(
            () => _sut.CreateAsync(new CreateFlashcardDeckRequest("Algorithmen", null, 20, 200)));
        _deckRepository.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithArchivedCourse_Throws()
    {
        var course = SetupCourse(isArchived: true);

        await Assert.ThrowsAsync<CourseArchivedException>(
            () => _sut.CreateAsync(new CreateFlashcardDeckRequest("Algorithmen", course.Id, 20, 200)));
    }

    [Fact]
    public async Task CreateAsync_WithUnknownCourse_Throws()
    {
        await Assert.ThrowsAsync<CourseNotFoundException>(
            () => _sut.CreateAsync(new CreateFlashcardDeckRequest("Algorithmen", Guid.NewGuid(), 20, 200)));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidLimit_Throws()
    {
        await Assert.ThrowsAsync<FlashcardValidationException>(
            () => _sut.CreateAsync(new CreateFlashcardDeckRequest("Algorithmen", null, -1, 200)));
    }

    [Fact]
    public async Task UpdateAsync_KeepingAnArchivedCourse_IsAllowed()
    {
        var course = SetupCourse(isArchived: true);
        var deck = SetupDeck(courseId: course.Id);

        var result = await _sut.UpdateAsync(new UpdateFlashcardDeckRequest(deck.Id, "Graphen", course.Id, 5, 50));

        Assert.Equal("Graphen", result.Name);
        _deckRepository.Verify(r => r.Update(It.Is<FlashcardDeck>(d => d.Id == deck.Id && d.NewCardsPerDay == 5)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ArchivedDeck_Throws()
    {
        var deck = SetupDeck(isArchived: true);

        await Assert.ThrowsAsync<FlashcardDeckArchivedException>(
            () => _sut.UpdateAsync(new UpdateFlashcardDeckRequest(deck.Id, "Graphen", null, 20, 200)));
    }

    [Fact]
    public async Task UpdateAsync_WithNameOfAnotherDeck_Throws()
    {
        var deck = SetupDeck();
        _deckRepository.Setup(r => r.ExistsByNameAsync("Netze", deck.Id, default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<DuplicateFlashcardDeckNameException>(
            () => _sut.UpdateAsync(new UpdateFlashcardDeckRequest(deck.Id, "Netze", null, 20, 200)));
    }

    [Fact]
    public async Task ArchiveAsync_ThenRestoreAsync_TogglesTheFlag()
    {
        var deck = SetupDeck();

        var archived = await _sut.ArchiveAsync(deck.Id);
        _deckRepository.Setup(r => r.GetByIdAsync(deck.Id, default)).ReturnsAsync(deck with { IsArchived = true });
        var restored = await _sut.RestoreAsync(deck.Id);

        Assert.True(archived.IsArchived);
        Assert.False(restored.IsArchived);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_Throws()
    {
        await Assert.ThrowsAsync<FlashcardDeckNotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }
}
