using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Flashcards;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Business.Flashcards;

public class FlashcardStudyOrchestratorTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TodayStart = new(2026, 10, 8, 2, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TomorrowStart = new(2026, 10, 9, 2, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IFlashcardDeckRepository> _deckRepository = new();
    private readonly Mock<IFlashcardRepository> _flashcardRepository = new();
    private readonly FlashcardStudyOrchestrator _sut;
    private readonly FlashcardDeck _deck = new(Guid.NewGuid(), "Algorithmen", null, null, 20, 200, false, Now, Now);

    public FlashcardStudyOrchestratorTests()
    {
        var studyDayProvider = new StudyDayProvider(new FlashcardStudyOptions(), new FixedTimeProvider(Now));
        _sut = new FlashcardStudyOrchestrator(
            _deckRepository.Object,
            _flashcardRepository.Object,
            new FlashcardReviewProcessor(studyDayProvider),
            new StudyQueueProvider(),
            studyDayProvider);

        _deckRepository.Setup(r => r.GetByIdAsync(_deck.Id, default)).ReturnsAsync(_deck);
        SetupCounts(new FlashcardDeckCardCounts(_deck.Id, 3, 1, 0, 2));
    }

    private void SetupCounts(FlashcardDeckCardCounts cardCounts, FlashcardDeckReviewCounts? reviewCounts = null)
    {
        _flashcardRepository.Setup(r => r.GetCardCountsAsync(TomorrowStart, _deck.Id, default)).ReturnsAsync([cardCounts]);
        _flashcardRepository.Setup(r => r.GetReviewCountsAsync(TodayStart, _deck.Id, default))
            .ReturnsAsync(reviewCounts is null ? [] : [reviewCounts]);
    }

    private Flashcard Card(FlashcardState state, DateTime dueAt, int intervalDays = 0) =>
        new(Guid.NewGuid(), _deck.Id, $"Q {state}", "A", "graphen", null, state, 0, dueAt, intervalDays, 2500, 0, 0, null, Now, Now);

    private void SetupQueue(Flashcard? learning = null, Flashcard? review = null, Flashcard? newCard = null)
    {
        _flashcardRepository.Setup(r => r.GetFirstLearningCardAsync(_deck.Id, default)).ReturnsAsync(learning);
        _flashcardRepository.Setup(r => r.GetFirstReviewCardAsync(_deck.Id, TomorrowStart, default)).ReturnsAsync(review);
        _flashcardRepository.Setup(r => r.GetFirstNewCardAsync(_deck.Id, default)).ReturnsAsync(newCard);
    }

    [Fact]
    public async Task GetNextAsync_ReturnsDueReviewWithCountsAndButtonIntervals()
    {
        var review = Card(FlashcardState.Review, TodayStart, intervalDays: 10);
        SetupQueue(review: review, newCard: Card(FlashcardState.New, Now));

        var result = await _sut.GetNextAsync(_deck.Id);

        Assert.Equal(review.Id, result!.CardId);
        Assert.Equal("Algorithmen", result.DeckName);
        Assert.Equal(["graphen"], result.Tags);
        Assert.Equal(new FlashcardStudyCountsDto(1, 0, 2), result.Counts);
        Assert.Equal(
            [TimeSpan.FromMinutes(10), TimeSpan.FromDays(12), TimeSpan.FromDays(25), TimeSpan.FromDays(33)],
            result.Intervals.Select(i => i.Interval));
    }

    [Fact]
    public async Task GetNextAsync_WhenNewCardLimitIsUsedUp_ReturnsNull()
    {
        SetupCounts(new FlashcardDeckCardCounts(_deck.Id, 30, 30, 0, 0), new FlashcardDeckReviewCounts(_deck.Id, 20, 0));
        SetupQueue(newCard: Card(FlashcardState.New, Now));

        Assert.Null(await _sut.GetNextAsync(_deck.Id));
    }

    [Fact]
    public async Task GetNextAsync_ForArchivedDeck_Throws()
    {
        _deckRepository.Setup(r => r.GetByIdAsync(_deck.Id, default)).ReturnsAsync(_deck with { IsArchived = true });

        await Assert.ThrowsAsync<FlashcardDeckArchivedException>(() => _sut.GetNextAsync(_deck.Id));
    }

    [Fact]
    public async Task AnswerAsync_StoresRescheduledCardAndReviewLogThenReturnsNextCard()
    {
        var newCard = Card(FlashcardState.New, Now);
        var next = Card(FlashcardState.Review, TodayStart, intervalDays: 3);
        _flashcardRepository.Setup(r => r.GetByIdAsync(newCard.Id, default)).ReturnsAsync(newCard);
        SetupQueue(review: next);

        var result = await _sut.AnswerAsync(new AnswerFlashcardRequest(newCard.Id, FlashcardRating.Good));

        _flashcardRepository.Verify(r => r.Update(It.Is<Flashcard>(c =>
            c.Id == newCard.Id && c.State == FlashcardState.Learning && c.Step == 1 && c.DueAt == Now.AddMinutes(10))), Times.Once);
        _flashcardRepository.Verify(r => r.AddReviewAsync(It.Is<FlashcardReview>(review =>
            review.FlashcardId == newCard.Id && review.Rating == FlashcardRating.Good && review.StateBefore == FlashcardState.New), default), Times.Once);
        _flashcardRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
        Assert.Equal(next.Id, result!.CardId);
    }

    [Fact]
    public async Task AnswerAsync_ForReviewCardDueNextWeek_ThrowsNotDue()
    {
        var card = Card(FlashcardState.Review, TodayStart.AddDays(7), intervalDays: 10);
        _flashcardRepository.Setup(r => r.GetByIdAsync(card.Id, default)).ReturnsAsync(card);

        await Assert.ThrowsAsync<FlashcardNotDueException>(() => _sut.AnswerAsync(new AnswerFlashcardRequest(card.Id, FlashcardRating.Good)));
        _flashcardRepository.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task AnswerAsync_WithUnknownRating_Throws()
    {
        await Assert.ThrowsAsync<FlashcardValidationException>(
            () => _sut.AnswerAsync(new AnswerFlashcardRequest(Guid.NewGuid(), (FlashcardRating)7)));
    }

    [Fact]
    public async Task AnswerAsync_WithUnknownCard_Throws()
    {
        await Assert.ThrowsAsync<FlashcardNotFoundException>(
            () => _sut.AnswerAsync(new AnswerFlashcardRequest(Guid.NewGuid(), FlashcardRating.Good)));
    }
}
