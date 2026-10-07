using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Domain.Flashcards;

public class FlashcardImportProcessorTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);

    private static readonly FlashcardDeck Algorithms = new(
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
    private static readonly FlashcardDeck Archived = new(
        Guid.NewGuid(),
        "Altes Deck",
        null,
        null,
        20,
        200,
        true,
        Now,
        Now
    );
    private static readonly IReadOnlyList<FlashcardDeck> Decks = [Algorithms, Archived];

    private readonly FlashcardImportProcessor _sut;

    public FlashcardImportProcessorTests()
    {
        var timeProvider = new FixedTimeProvider(Now);
        var validator = new FlashcardValidator();
        _sut = new FlashcardImportProcessor(
            validator,
            new FlashcardDeckLifecycle(timeProvider),
            new FlashcardLifecycle(validator, timeProvider)
        );
    }

    private static AnkiCsvRow Row(
        int line,
        string front,
        string back = "Antwort",
        string? deck = null,
        string? noteType = null,
        params string[] tags
    ) => new(line, front, back, tags, deck, noteType);

    private static AnkiCsvParseResult File(params AnkiCsvRow[] rows) => new(null, rows);

    private static FlashcardImportTarget Target(
        ImportDuplicateMode mode = ImportDuplicateMode.UpdateCurrent,
        FlashcardDeck? deck = null
    ) => new(deck ?? Algorithms, "export.txt", mode);

    private static Flashcard ExistingCard(string front, FlashcardDeck? deck = null) =>
        new(
            Guid.NewGuid(),
            (deck ?? Algorithms).Id,
            front,
            "Alte Antwort",
            "alt",
            null,
            FlashcardState.Review,
            0,
            Now.AddDays(3),
            12,
            2350,
            8,
            1,
            Now,
            Now,
            Now
        );

    [Fact]
    public void Process_AddsRowsToTheTargetDeckInFileOrder()
    {
        var outcome = _sut.Process(File(Row(1, "Q1"), Row(2, "Q2")), Target(), Decks, []);

        Assert.Equal(["Q1", "Q2"], outcome.AddedCards.Select(c => c.Front));
        Assert.All(outcome.AddedCards, card => Assert.Equal(Algorithms.Id, card.DeckId));
        Assert.True(outcome.AddedCards[0].DueAt < outcome.AddedCards[1].DueAt);
        Assert.Empty(outcome.CreatedDecks);
        Assert.Equal(
            [new ImportedFlashcardDeckDto(Algorithms.Id, "Algorithmen", false)],
            outcome.Decks
        );
    }

    [Fact]
    public void Process_WithoutTargetDeck_CreatesDeckNamedAfterTheFile()
    {
        var outcome = _sut.Process(
            File(Row(1, "Q1")),
            new FlashcardImportTarget(null, "Netze  WS25.csv", ImportDuplicateMode.UpdateCurrent),
            Decks,
            []
        );

        var deck = Assert.Single(outcome.CreatedDecks);
        Assert.Equal("Netze WS25", deck.Name);
        Assert.Equal(deck.Id, Assert.Single(outcome.AddedCards).DeckId);
        Assert.True(Assert.Single(outcome.Decks).IsNew);
    }

    [Fact]
    public void Process_WithoutTargetDeck_ReusesExistingDeckWithTheFileName()
    {
        var outcome = _sut.Process(
            File(Row(1, "Q1")),
            new FlashcardImportTarget(null, "algorithmen.txt", ImportDuplicateMode.UpdateCurrent),
            Decks,
            []
        );

        Assert.Empty(outcome.CreatedDecks);
        Assert.Equal(Algorithms.Id, Assert.Single(outcome.AddedCards).DeckId);
    }

    [Fact]
    public void Process_RowDeckWinsOverTheDialogAndIsFoundIgnoringCase()
    {
        var outcome = _sut.Process(
            File(
                Row(1, "Q1", deck: "ALGORITHMEN"),
                Row(2, "Q2", deck: "Informatik::Netze"),
                Row(3, "Q3", deck: "informatik::netze")
            ),
            Target(deck: null),
            Decks,
            []
        );

        var created = Assert.Single(outcome.CreatedDecks);
        Assert.Equal("Informatik::Netze", created.Name);
        Assert.Equal(
            [Algorithms.Id, created.Id, created.Id],
            outcome.AddedCards.OrderBy(c => c.Front).Select(c => c.DeckId)
        );
    }

    [Fact]
    public void Process_UpdateCurrent_OverwritesBackAndTagsAndKeepsProgress()
    {
        var existing = ExistingCard("Q1");

        var outcome = _sut.Process(
            File(Row(1, " Q1 ", "Neue Antwort", tags: "neu")),
            Target(ImportDuplicateMode.UpdateCurrent),
            Decks,
            [existing]
        );

        var updated = Assert.Single(outcome.UpdatedCards);
        Assert.Equal("Neue Antwort", updated.Back);
        Assert.Equal("neu", updated.Tags);
        Assert.Equal(existing.State, updated.State);
        Assert.Equal(existing.IntervalDays, updated.IntervalDays);
        Assert.Equal(existing.EaseFactor, updated.EaseFactor);
        Assert.Equal(existing.DueAt, updated.DueAt);
        Assert.Empty(outcome.AddedCards);
    }

    [Fact]
    public void Process_KeepCurrent_SkipsDuplicates()
    {
        var outcome = _sut.Process(
            File(Row(1, "Q1"), Row(2, "Q2")),
            Target(ImportDuplicateMode.KeepCurrent),
            Decks,
            [ExistingCard("Q1")]
        );

        Assert.Equal(1, outcome.SkippedDuplicates);
        Assert.Equal("Q2", Assert.Single(outcome.AddedCards).Front);
        Assert.Empty(outcome.UpdatedCards);
    }

    [Fact]
    public void Process_KeepBoth_AddsDuplicateAsNewCard()
    {
        var outcome = _sut.Process(
            File(Row(1, "Q1")),
            Target(ImportDuplicateMode.KeepBoth),
            Decks,
            [ExistingCard("Q1")]
        );

        Assert.Equal("Q1", Assert.Single(outcome.AddedCards).Front);
        Assert.Empty(outcome.UpdatedCards);
    }

    [Fact]
    public void Process_FileDuplicateModeWinsOverTheDialog()
    {
        var file = new AnkiCsvParseResult(ImportDuplicateMode.KeepCurrent, [Row(1, "Q1")]);

        var outcome = _sut.Process(
            file,
            Target(ImportDuplicateMode.UpdateCurrent),
            Decks,
            [ExistingCard("Q1")]
        );

        Assert.Equal(1, outcome.SkippedDuplicates);
    }

    [Fact]
    public void Process_DuplicateRowsInsideTheFile_UpdateTheCardAddedEarlier()
    {
        var outcome = _sut.Process(
            File(Row(1, "Q1", "erste"), Row(2, "Q1", "zweite")),
            Target(),
            Decks,
            []
        );

        Assert.Equal("zweite", Assert.Single(outcome.AddedCards).Back);
    }

    [Fact]
    public void Process_DuplicateCheckIsPerDeck()
    {
        var outcome = _sut.Process(
            File(Row(1, "Q1", deck: "Netze")),
            Target(),
            Decks,
            [ExistingCard("Q1")]
        );

        Assert.Single(outcome.AddedCards);
        Assert.Empty(outcome.UpdatedCards);
    }

    [Fact]
    public void Process_ReportsBadRowsWithLineNumbersAndImportsTheRest()
    {
        var outcome = _sut.Process(
            File(
                Row(3, "Ein {{c1::B-Baum}}", "", noteType: "Cloze"),
                Row(4, "", "Antwort"),
                Row(5, "Q", new string('x', FlashcardDto.BackMaxLength + 1)),
                Row(6, "Q6", deck: "Altes Deck"),
                Row(7, "Q7", deck: new string('d', FlashcardDeck.NameMaxLength + 1)),
                Row(8, "Q8", noteType: "Basic (and reversed card)")
            ),
            Target(),
            Decks,
            []
        );

        Assert.Equal([3, 4, 5, 6, 7], outcome.Failures.Select(f => f.LineNumber));
        Assert.Equal("Cloze notes are not supported yet.", outcome.Failures[0].Reason);
        Assert.Equal("Front is required.", outcome.Failures[1].Reason);
        Assert.Contains("archived", outcome.Failures[3].Reason);
        Assert.Equal("Q8", Assert.Single(outcome.AddedCards).Front);
    }

    [Fact]
    public void Process_WithNoUsableRow_ThrowsWithTheFirstReason()
    {
        var ex = Assert.Throws<FlashcardImportException>(() =>
            _sut.Process(File(Row(2, "Lücke", noteType: "Cloze")), Target(), Decks, [])
        );

        Assert.Contains("Line 2: Cloze notes are not supported yet.", ex.Message);
    }

    [Fact]
    public void Process_WithEmptyFile_Throws()
    {
        Assert.Throws<FlashcardImportException>(() => _sut.Process(File(), Target(), Decks, []));
    }

    [Fact]
    public void GetTargetDeckIds_ReturnsExistingDecksTheRowsGoTo()
    {
        var ids = _sut.GetTargetDeckIds(
            File(Row(1, "Q1"), Row(2, "Q2", deck: "algorithmen"), Row(3, "Q3", deck: "Neu")),
            Target(deck: Archived),
            Decks
        );

        Assert.Equal([Archived.Id, Algorithms.Id], ids);
    }
}
