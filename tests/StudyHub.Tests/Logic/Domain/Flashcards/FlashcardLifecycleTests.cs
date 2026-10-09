using StudyHub.Logic.Domain;
using StudyHub.Shared.Flashcards;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Domain.Flashcards;

public class FlashcardLifecycleTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid DeckId = Guid.NewGuid();

    private readonly FixedTimeProvider _timeProvider = new(Now);
    private readonly FlashcardLifecycle _sut;

    public FlashcardLifecycleTests()
    {
        _sut = new FlashcardLifecycle(new FlashcardValidator(), _timeProvider);
    }

    [Fact]
    public void Create_ReturnsNewCardsInTheGivenQueueOrder()
    {
        var noteId = Guid.NewGuid();

        var cards = _sut.Create(
            DeckId,
            [new FlashcardDto(" Q1 ", "A1", []), new FlashcardDto("Q2", "A2", [])],
            noteId
        );

        Assert.Equal(["Q1", "Q2"], cards.Select(c => c.Front));
        Assert.All(
            cards,
            card =>
            {
                Assert.Equal(DeckId, card.DeckId);
                Assert.Equal(noteId, card.SourceNoteId);
                Assert.Equal(FlashcardState.New, card.State);
                Assert.Equal(2500, card.EaseFactor);
                Assert.Equal(0, card.Reps);
                Assert.Equal(Now, card.CreatedAt);
            }
        );
        Assert.True(cards[0].DueAt < cards[1].DueAt);
    }

    [Fact]
    public void Create_StoresTagsSpaceSeparatedWithoutInnerWhitespace()
    {
        var card = Assert.Single(
            _sut.Create(
                DeckId,
                [new FlashcardDto("Q", "A", ["Kürzeste Wege", "graphen", "Graphen"])],
                null
            )
        );

        Assert.Equal("Kürzeste_Wege graphen", card.Tags);
    }

    [Fact]
    public void Create_WithoutTags_StoresNull()
    {
        Assert.Null(
            Assert.Single(_sut.Create(DeckId, [new FlashcardDto("Q", "A", [])], null)).Tags
        );
    }

    [Fact]
    public void Create_WithInvalidCard_ThrowsWithCardNumber()
    {
        var ex = Assert.Throws<FlashcardValidationException>(() =>
            _sut.Create(
                DeckId,
                [new FlashcardDto("Q1", "A1", []), new FlashcardDto("Q2", "", [])],
                null
            )
        );

        Assert.StartsWith("Card 2:", ex.Message);
    }

    [Fact]
    public void UpdateContent_KeepsLearningProgress()
    {
        var card = new Flashcard(
            Guid.NewGuid(),
            DeckId,
            "Q",
            "A",
            null,
            null,
            FlashcardState.Review,
            0,
            Now.AddDays(3),
            7,
            2350,
            5,
            1,
            Now,
            Now,
            Now
        );
        _timeProvider.UtcNow = Now.AddHours(2);

        var updated = _sut.UpdateContent(
            card,
            new FlashcardDto("Neue Frage", "Neue Antwort", ["netze"])
        );

        Assert.Equal("Neue Frage", updated.Front);
        Assert.Equal("Neue Antwort", updated.Back);
        Assert.Equal("netze", updated.Tags);
        Assert.Equal(
            card with
            {
                Front = updated.Front,
                Back = updated.Back,
                Tags = updated.Tags,
                UpdatedAt = Now.AddHours(2),
            },
            updated
        );
    }

    [Fact]
    public void UpdateContent_WithEmptyFront_Throws()
    {
        var card = Assert.Single(_sut.Create(DeckId, [new FlashcardDto("Q", "A", [])], null));

        Assert.Throws<FlashcardValidationException>(() =>
            _sut.UpdateContent(card, new FlashcardDto(" ", "A", []))
        );
    }
}
