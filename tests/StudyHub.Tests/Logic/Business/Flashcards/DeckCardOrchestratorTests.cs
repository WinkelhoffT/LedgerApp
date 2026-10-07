using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Business.Flashcards;

public class DeckCardOrchestratorTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IFlashcardDeckRepository> _deckRepository = new();
    private readonly Mock<IFlashcardRepository> _flashcardRepository = new();
    private readonly Mock<INoteRepository> _noteRepository = new();
    private readonly DeckCardOrchestrator _sut;

    public DeckCardOrchestratorTests()
    {
        var timeProvider = new FixedTimeProvider(Now);
        var validator = new FlashcardValidator();
        _sut = new DeckCardOrchestrator(
            _deckRepository.Object,
            _flashcardRepository.Object,
            _noteRepository.Object,
            validator,
            new FlashcardLifecycle(validator, timeProvider),
            new StudyDayProvider(new FlashcardStudyOptions(), timeProvider)
        );
    }

    private FlashcardDeck SetupDeck(bool isArchived = false)
    {
        var deck = new FlashcardDeck(
            Guid.NewGuid(),
            "Algorithmen",
            null,
            null,
            20,
            200,
            isArchived,
            Now,
            Now
        );
        _deckRepository.Setup(r => r.GetByIdAsync(deck.Id, default)).ReturnsAsync(deck);
        return deck;
    }

    private Flashcard SetupCard(
        FlashcardDeck deck,
        FlashcardState state = FlashcardState.Review,
        DateTime? dueAt = null
    )
    {
        var card = new Flashcard(
            Guid.NewGuid(),
            deck.Id,
            "Q",
            "A",
            "graphen kürzeste_wege",
            null,
            state,
            0,
            dueAt ?? Now,
            4,
            2500,
            2,
            0,
            Now,
            Now,
            Now
        );
        _flashcardRepository.Setup(r => r.GetByIdAsync(card.Id, default)).ReturnsAsync(card);
        return card;
    }

    [Fact]
    public async Task GetCardsAsync_ReturnsCardsWithTagsAndStudyDayDueDate()
    {
        var deck = SetupDeck();
        var review = SetupCard(deck, dueAt: new DateTime(2026, 10, 12, 2, 0, 0, DateTimeKind.Utc));
        var newCard = SetupCard(deck, FlashcardState.New);
        _flashcardRepository
            .Setup(r => r.GetByDeckIdAsync(deck.Id, "graph", default))
            .ReturnsAsync([review, newCard]);

        var result = await _sut.GetCardsAsync(deck.Id, "graph");

        Assert.Equal(new DateOnly(2026, 10, 12), result[0].DueDate);
        Assert.Equal(["graphen", "kürzeste_wege"], result[0].Tags);
        Assert.Null(result[1].DueDate);
    }

    [Fact]
    public async Task GetCardsAsync_WithUnknownDeck_Throws()
    {
        await Assert.ThrowsAsync<FlashcardDeckNotFoundException>(() =>
            _sut.GetCardsAsync(Guid.NewGuid(), null)
        );
    }

    [Fact]
    public async Task AddCardsAsync_WithSourceNote_SavesNewCardsInOrder()
    {
        var deck = SetupDeck();
        var noteId = Guid.NewGuid();
        _noteRepository
            .Setup(r => r.GetByIdAsync(noteId, default))
            .ReturnsAsync(
                new Note(noteId, "Dijkstra", "x", null, Guid.NewGuid(), null, false, Now, Now)
            );
        IReadOnlyCollection<Flashcard>? saved = null;
        _flashcardRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyCollection<Flashcard>>(), default))
            .Callback<IReadOnlyCollection<Flashcard>, CancellationToken>(
                (cards, _) => saved = cards
            );

        var result = await _sut.AddCardsAsync(
            new AddFlashcardsRequest(
                deck.Id,
                [new FlashcardDto("Q1", "A1", ["kürzeste wege"]), new FlashcardDto("Q2", "A2", [])],
                noteId
            )
        );

        Assert.Equal(["Q1", "Q2"], result.Select(c => c.Front));
        Assert.Equal(["kürzeste_wege"], result[0].Tags);
        Assert.All(
            saved!,
            card =>
            {
                Assert.Equal(noteId, card.SourceNoteId);
                Assert.Equal(FlashcardState.New, card.State);
            }
        );
        _flashcardRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task AddCardsAsync_WithUnknownSourceNote_Throws()
    {
        var deck = SetupDeck();

        await Assert.ThrowsAsync<NoteNotFoundException>(() =>
            _sut.AddCardsAsync(
                new AddFlashcardsRequest(deck.Id, [new FlashcardDto("Q", "A", [])], Guid.NewGuid())
            )
        );
    }

    [Fact]
    public async Task AddCardsAsync_ToArchivedDeck_Throws()
    {
        var deck = SetupDeck(isArchived: true);

        await Assert.ThrowsAsync<FlashcardDeckArchivedException>(() =>
            _sut.AddCardsAsync(
                new AddFlashcardsRequest(deck.Id, [new FlashcardDto("Q", "A", [])], null)
            )
        );
    }

    [Fact]
    public async Task AddCardsAsync_WithoutCards_Throws()
    {
        var deck = SetupDeck();

        await Assert.ThrowsAsync<FlashcardValidationException>(() =>
            _sut.AddCardsAsync(new AddFlashcardsRequest(deck.Id, [], null))
        );
    }

    [Fact]
    public async Task UpdateCardAsync_ChangesContentAndKeepsProgress()
    {
        var card = SetupCard(SetupDeck());

        var result = await _sut.UpdateCardAsync(
            new UpdateFlashcardRequest(card.Id, new FlashcardDto("Neu", "Antwort", []))
        );

        Assert.Equal("Neu", result.Front);
        Assert.Equal(card.IntervalDays, result.IntervalDays);
        _flashcardRepository.Verify(
            r =>
                r.Update(
                    It.Is<Flashcard>(c =>
                        c.Id == card.Id && c.Front == "Neu" && c.Reps == card.Reps
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateCardAsync_WithUnknownCard_Throws()
    {
        await Assert.ThrowsAsync<FlashcardNotFoundException>(() =>
            _sut.UpdateCardAsync(
                new UpdateFlashcardRequest(Guid.NewGuid(), new FlashcardDto("Q", "A", []))
            )
        );
    }

    [Fact]
    public async Task DeleteCardAsync_RemovesCard()
    {
        var card = SetupCard(SetupDeck());

        await _sut.DeleteCardAsync(card.Id);

        _flashcardRepository.Verify(r => r.Remove(card), Times.Once);
        _flashcardRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteCardAsync_InArchivedDeck_Throws()
    {
        var card = SetupCard(SetupDeck(isArchived: true));

        await Assert.ThrowsAsync<FlashcardDeckArchivedException>(() =>
            _sut.DeleteCardAsync(card.Id)
        );
    }
}
