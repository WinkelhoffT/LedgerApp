using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Tests.Logic.Domain.Flashcards;

public class StudyQueueProviderTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly FlashcardDeck Deck = new(
        Guid.NewGuid(),
        "Algorithmen",
        null,
        null,
        20,
        200,
        false,
        Now,
        Now
    );
    private static readonly FlashcardStudyCountsDto AllLeft = new(5, 1, 5);
    private static readonly StudyDay Today = new(
        Now,
        new DateOnly(2026, 10, 8),
        Now.Date.AddHours(2),
        Now.Date.AddDays(1).AddHours(2)
    );

    private readonly StudyQueueProvider _sut = new();

    private static Flashcard Card(FlashcardState state, DateTime dueAt) =>
        new(
            Guid.NewGuid(),
            Deck.Id,
            "Q",
            "A",
            null,
            null,
            state,
            0,
            dueAt,
            0,
            2500,
            0,
            0,
            null,
            Now,
            Now
        );

    [Fact]
    public void GetCounts_AppliesDailyLimitsCountedFromTodaysAnswers()
    {
        var counts = _sut.GetCounts(
            Deck with
            {
                NewCardsPerDay = 10,
                ReviewsPerDay = 50,
            },
            new FlashcardDeckCardCounts(Deck.Id, 200, 30, 3, 80),
            new FlashcardDeckReviewCounts(Deck.Id, 4, 45)
        );

        Assert.Equal(new FlashcardStudyCountsDto(6, 3, 5), counts);
    }

    [Fact]
    public void GetCounts_WithFewerCardsThanTheLimit_ShowsAllCards()
    {
        var counts = _sut.GetCounts(Deck, new FlashcardDeckCardCounts(Deck.Id, 7, 2, 0, 5), null);

        Assert.Equal(new FlashcardStudyCountsDto(2, 0, 5), counts);
    }

    [Fact]
    public void GetCounts_WithLimitAlreadyExceeded_NeverGoesNegative()
    {
        var counts = _sut.GetCounts(
            Deck with
            {
                NewCardsPerDay = 5,
            },
            new FlashcardDeckCardCounts(Deck.Id, 30, 30, 0, 0),
            new FlashcardDeckReviewCounts(Deck.Id, 8, 0)
        );

        Assert.Equal(0, counts.New);
    }

    [Fact]
    public void GetCounts_ForArchivedDeck_IsEmpty()
    {
        var counts = _sut.GetCounts(
            Deck with
            {
                IsArchived = true,
            },
            new FlashcardDeckCardCounts(Deck.Id, 7, 2, 1, 5),
            null
        );

        Assert.Equal(FlashcardStudyCountsDto.Empty, counts);
    }

    [Fact]
    public void SelectNext_PrefersLearningCardThatIsDueNow()
    {
        var learning = Card(FlashcardState.Learning, Now.AddSeconds(-1));
        var candidates = new StudyQueueCandidates(
            learning,
            Card(FlashcardState.Review, Now.AddDays(-2)),
            Card(FlashcardState.New, Now)
        );

        Assert.Same(learning, _sut.SelectNext(Deck, candidates, AllLeft, Today));
    }

    [Fact]
    public void SelectNext_ShowsReviewsBeforeNewCardsAndLearningAhead()
    {
        var review = Card(FlashcardState.Review, Now.AddDays(-2));
        var candidates = new StudyQueueCandidates(
            Card(FlashcardState.Learning, Now.AddMinutes(5)),
            review,
            Card(FlashcardState.New, Now)
        );

        Assert.Same(review, _sut.SelectNext(Deck, candidates, AllLeft, Today));
    }

    [Fact]
    public void SelectNext_WithReviewLimitReached_ShowsNewCard()
    {
        var newCard = Card(FlashcardState.New, Now);
        var candidates = new StudyQueueCandidates(
            null,
            Card(FlashcardState.Review, Now.AddDays(-2)),
            newCard
        );

        Assert.Same(newCard, _sut.SelectNext(Deck, candidates, AllLeft with { Review = 0 }, Today));
    }

    [Fact]
    public void SelectNext_WithNothingElseLeft_LearnsAheadWithinTwentyMinutes()
    {
        var learning = Card(FlashcardState.Learning, Now.AddMinutes(20));
        var candidates = new StudyQueueCandidates(learning, null, Card(FlashcardState.New, Now));

        Assert.Same(learning, _sut.SelectNext(Deck, candidates, AllLeft with { New = 0 }, Today));
    }

    [Fact]
    public void SelectNext_WithLearningCardBeyondLearnAheadLimit_IsFinishedForNow()
    {
        var candidates = new StudyQueueCandidates(
            Card(FlashcardState.Relearning, Now.AddMinutes(21)),
            null,
            null
        );

        Assert.Null(_sut.SelectNext(Deck, candidates, AllLeft, Today));
    }

    [Fact]
    public void SelectNext_ForArchivedDeck_ReturnsNull()
    {
        var candidates = new StudyQueueCandidates(
            Card(FlashcardState.Learning, Now.AddMinutes(-1)),
            null,
            null
        );

        Assert.Null(_sut.SelectNext(Deck with { IsArchived = true }, candidates, AllLeft, Today));
    }

    [Theory]
    [InlineData(FlashcardState.Learning, 20, true)]
    [InlineData(FlashcardState.Relearning, 21, false)]
    public void IsDue_LearningCard_DependsOnTheLearnAheadLimit(
        FlashcardState state,
        int dueInMinutes,
        bool expected
    )
    {
        Assert.Equal(
            expected,
            _sut.IsDue(Deck, Card(state, Now.AddMinutes(dueInMinutes)), AllLeft, Today)
        );
    }

    [Fact]
    public void IsDue_ReviewCard_MustBeDueTodayWithinTheLimit()
    {
        Assert.True(
            _sut.IsDue(
                Deck,
                Card(FlashcardState.Review, Today.NextStart.AddTicks(-1)),
                AllLeft,
                Today
            )
        );
        Assert.False(
            _sut.IsDue(Deck, Card(FlashcardState.Review, Today.NextStart), AllLeft, Today)
        );
        Assert.False(
            _sut.IsDue(Deck, Card(FlashcardState.Review, Now), AllLeft with { Review = 0 }, Today)
        );
    }

    [Fact]
    public void IsDue_NewCard_DependsOnTheNewCardLimit()
    {
        Assert.True(_sut.IsDue(Deck, Card(FlashcardState.New, Now), AllLeft, Today));
        Assert.False(
            _sut.IsDue(Deck, Card(FlashcardState.New, Now), AllLeft with { New = 0 }, Today)
        );
    }

    [Fact]
    public void IsDue_CardOfAnotherOrArchivedDeck_IsFalse()
    {
        var card = Card(FlashcardState.New, Now);

        Assert.False(_sut.IsDue(Deck with { Id = Guid.NewGuid() }, card, AllLeft, Today));
        Assert.False(_sut.IsDue(Deck with { IsArchived = true }, card, AllLeft, Today));
    }
}
