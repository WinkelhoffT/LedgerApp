using Microsoft.EntityFrameworkCore;
using StudyHub.Data;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Tests.Data.Flashcards;

public class FlashcardRepositoryTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TomorrowStart = new(2026, 10, 9, 2, 0, 0, DateTimeKind.Utc);
    private static readonly Guid DeckId = Guid.NewGuid();
    private static readonly Guid OtherDeckId = Guid.NewGuid();

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Flashcard Card(
        FlashcardState state,
        DateTime dueAt,
        Guid? deckId = null,
        string front = "Front",
        string back = "Back",
        string? tags = null) =>
        new(Guid.NewGuid(), deckId ?? DeckId, front, back, tags, null, state, 0, dueAt, 0, 2500, 0, 0, null, Now, Now);

    private static FlashcardReview Review(Guid cardId, FlashcardState stateBefore, DateTime reviewedAt) =>
        new(Guid.NewGuid(), cardId, reviewedAt, FlashcardRating.Good, stateBefore, 0, 1, 2500);

    private static async Task<FlashcardRepository> SeedAsync(ApplicationDbContext dbContext, params Flashcard[] cards)
    {
        var repository = new FlashcardRepository(dbContext);
        await repository.AddRangeAsync(cards);
        await repository.SaveChangesAsync();
        return repository;
    }

    [Fact]
    public async Task GetCardCountsAsync_CountsNewAndCardsDueBeforeTheGivenTimePerDeck()
    {
        await using var dbContext = CreateDbContext();
        var repository = await SeedAsync(
            dbContext,
            Card(FlashcardState.New, Now),
            Card(FlashcardState.New, Now),
            Card(FlashcardState.Learning, Now.AddMinutes(10)),
            Card(FlashcardState.Relearning, Now.AddMinutes(5)),
            Card(FlashcardState.Review, TomorrowStart.AddDays(-1)),
            Card(FlashcardState.Review, TomorrowStart),
            Card(FlashcardState.New, Now, OtherDeckId));

        var counts = await repository.GetCardCountsAsync(TomorrowStart);

        var deck = Assert.Single(counts, c => c.DeckId == DeckId);
        Assert.Equal(new FlashcardDeckCardCounts(DeckId, 6, 2, 2, 1), deck);
        Assert.Equal(new FlashcardDeckCardCounts(OtherDeckId, 1, 1, 0, 0), Assert.Single(counts, c => c.DeckId == OtherDeckId));
    }

    [Fact]
    public async Task GetCardCountsAsync_WithDeckId_ReturnsOnlyThatDeck()
    {
        await using var dbContext = CreateDbContext();
        var repository = await SeedAsync(dbContext, Card(FlashcardState.New, Now), Card(FlashcardState.New, Now, OtherDeckId));

        var counts = await repository.GetCardCountsAsync(TomorrowStart, DeckId);

        Assert.Equal(DeckId, Assert.Single(counts).DeckId);
    }

    [Fact]
    public async Task GetReviewCountsAsync_CountsTodaysAnswersToNewAndReviewCards()
    {
        await using var dbContext = CreateDbContext();
        var newCard = Card(FlashcardState.Learning, Now);
        var reviewCard = Card(FlashcardState.Review, TomorrowStart);
        var otherDeckCard = Card(FlashcardState.Learning, Now, OtherDeckId);
        var repository = await SeedAsync(dbContext, newCard, reviewCard, otherDeckCard);
        var dayStart = TomorrowStart.AddDays(-1);
        await repository.AddReviewAsync(Review(newCard.Id, FlashcardState.New, dayStart.AddHours(1)));
        await repository.AddReviewAsync(Review(newCard.Id, FlashcardState.Learning, dayStart.AddHours(2)));
        await repository.AddReviewAsync(Review(reviewCard.Id, FlashcardState.Review, dayStart.AddHours(3)));
        await repository.AddReviewAsync(Review(reviewCard.Id, FlashcardState.Review, dayStart.AddMinutes(-1)));
        await repository.AddReviewAsync(Review(otherDeckCard.Id, FlashcardState.New, dayStart.AddHours(1)));
        await repository.SaveChangesAsync();

        var counts = await repository.GetReviewCountsAsync(dayStart, DeckId);

        Assert.Equal(new FlashcardDeckReviewCounts(DeckId, 1, 1), Assert.Single(counts));
    }

    [Fact]
    public async Task GetFirstLearningCardAsync_ReturnsEarliestDueLearningOrRelearningCard()
    {
        await using var dbContext = CreateDbContext();
        var relearning = Card(FlashcardState.Relearning, Now.AddMinutes(2));
        var repository = await SeedAsync(
            dbContext,
            Card(FlashcardState.Learning, Now.AddMinutes(9)),
            relearning,
            Card(FlashcardState.Review, Now.AddMinutes(-60)),
            Card(FlashcardState.Learning, Now, OtherDeckId));

        var card = await repository.GetFirstLearningCardAsync(DeckId);

        Assert.Equal(relearning.Id, card!.Id);
    }

    [Fact]
    public async Task GetFirstReviewCardAsync_ReturnsOldestDueReviewCard()
    {
        await using var dbContext = CreateDbContext();
        var oldest = Card(FlashcardState.Review, Now.AddDays(-3));
        var repository = await SeedAsync(dbContext, Card(FlashcardState.Review, Now.AddDays(-1)), oldest);

        var card = await repository.GetFirstReviewCardAsync(DeckId, TomorrowStart);

        Assert.Equal(oldest.Id, card!.Id);
    }

    [Fact]
    public async Task GetFirstReviewCardAsync_WithNothingDueToday_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var repository = await SeedAsync(dbContext, Card(FlashcardState.Review, TomorrowStart));

        Assert.Null(await repository.GetFirstReviewCardAsync(DeckId, TomorrowStart));
    }

    [Fact]
    public async Task GetFirstNewCardAsync_ReturnsCardAddedFirst()
    {
        await using var dbContext = CreateDbContext();
        var first = Card(FlashcardState.New, Now);
        var repository = await SeedAsync(dbContext, Card(FlashcardState.New, Now.AddTicks(1)), first);

        var card = await repository.GetFirstNewCardAsync(DeckId);

        Assert.Equal(first.Id, card!.Id);
    }

    [Fact]
    public async Task GetByDeckIdAsync_WithSearch_MatchesFrontBackAndTagsIgnoringCase()
    {
        await using var dbContext = CreateDbContext();
        var repository = await SeedAsync(
            dbContext,
            Card(FlashcardState.New, Now, front: "Was ist Dijkstra?"),
            Card(FlashcardState.New, Now, back: "Kürzeste Wege mit DIJKSTRA"),
            Card(FlashcardState.New, Now, tags: "graphen dijkstra"),
            Card(FlashcardState.New, Now, front: "B-Baum"));

        var cards = await repository.GetByDeckIdAsync(DeckId, "dijkstra");

        Assert.Equal(3, cards.Count);
    }

    [Fact]
    public async Task Remove_DeletesCard()
    {
        await using var dbContext = CreateDbContext();
        var card = Card(FlashcardState.New, Now);
        var repository = await SeedAsync(dbContext, card);
        dbContext.ChangeTracker.Clear();

        repository.Remove((await repository.GetByIdAsync(card.Id))!);
        await repository.SaveChangesAsync();

        Assert.Null(await repository.GetByIdAsync(card.Id));
    }
}
