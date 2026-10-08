using System.Text;
using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Shared.Flashcards;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Business.Flashcards;

public class FlashcardTransferOrchestratorTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IFlashcardDeckRepository> _deckRepository = new();
    private readonly Mock<IFlashcardRepository> _flashcardRepository = new();
    private readonly FlashcardTransferOrchestrator _sut;
    private readonly FlashcardDeck _deck = new(Guid.NewGuid(), "Informatik::Algorithmen", null, null, 20, 200, false, Now, Now);

    public FlashcardTransferOrchestratorTests()
    {
        var timeProvider = new FixedTimeProvider(Now);
        var validator = new FlashcardValidator();
        _sut = new FlashcardTransferOrchestrator(
            _deckRepository.Object,
            _flashcardRepository.Object,
            new AnkiCsvParser(),
            new FlashcardImportProcessor(validator, new FlashcardDeckLifecycle(timeProvider), new FlashcardLifecycle(validator, timeProvider)),
            new AnkiCsvSerializer());

        _deckRepository.Setup(r => r.GetByIdAsync(_deck.Id, default)).ReturnsAsync(_deck);
        _deckRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([_deck]);
    }

    private static ImportFlashcardsRequest Request(string csv, Guid? targetDeckId, ImportDuplicateMode mode = ImportDuplicateMode.UpdateCurrent) =>
        new("algorithmen.txt", Encoding.UTF8.GetBytes(csv), targetDeckId, mode);

    [Fact]
    public async Task ImportAsync_WithMixedRows_StoresDecksAndCardsInOneSave()
    {
        var existing = new Flashcard(Guid.NewGuid(), _deck.Id, "Dijkstra?", "alt", null, null, FlashcardState.Review, 0, Now, 5, 2500, 3, 0, Now, Now, Now);
        _flashcardRepository.Setup(r => r.GetByDeckIdsAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(_deck.Id)), default))
            .ReturnsAsync([existing]);
        var csv = "#separator:Semicolon\n#deck column:3\nDijkstra?;neu;\nNeu?;Antwort;\nNetze?;Antwort;Netze\n;leer;\n";

        var result = await _sut.ImportAsync(Request(csv, _deck.Id));

        Assert.Equal(2, result.Added);
        Assert.Equal(1, result.Updated);
        Assert.Equal(0, result.SkippedDuplicates);
        Assert.Equal(6, Assert.Single(result.Failures).LineNumber);
        Assert.Contains(result.Decks, d => d.Name == "Netze" && d.IsNew);
        _deckRepository.Verify(r => r.AddAsync(It.Is<FlashcardDeck>(d => d.Name == "Netze"), default), Times.Once);
        _flashcardRepository.Verify(r => r.Update(It.Is<Flashcard>(c => c.Id == existing.Id && c.Back == "neu" && c.IntervalDays == 5)), Times.Once);
        _flashcardRepository.Verify(r => r.AddRangeAsync(It.Is<IReadOnlyCollection<Flashcard>>(cards => cards.Count == 2), default), Times.Once);
        _flashcardRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_IntoArchivedDeck_Throws()
    {
        _deckRepository.Setup(r => r.GetByIdAsync(_deck.Id, default)).ReturnsAsync(_deck with { IsArchived = true });

        await Assert.ThrowsAsync<FlashcardDeckArchivedException>(() => _sut.ImportAsync(Request("q;a\n", _deck.Id)));
    }

    [Fact]
    public async Task ImportAsync_WithNoUsableRow_ThrowsAndSavesNothing()
    {
        _flashcardRepository.Setup(r => r.GetByDeckIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), default)).ReturnsAsync([]);

        await Assert.ThrowsAsync<FlashcardImportException>(() => _sut.ImportAsync(Request("#notetype:Cloze\n{{c1::x}};\n", _deck.Id)));
        _flashcardRepository.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task ExportAsync_ReturnsAnkiCsvNamedAfterTheDeck()
    {
        var card = new Flashcard(Guid.NewGuid(), _deck.Id, "Q", "A", "graphen kürzeste_wege", null, FlashcardState.New, 0, Now, 0, 2500, 0, 0, null, Now, Now);
        _flashcardRepository.Setup(r => r.GetByDeckIdAsync(_deck.Id, null, default)).ReturnsAsync([card]);

        var export = await _sut.ExportAsync(_deck.Id);

        var csv = Encoding.UTF8.GetString(export.Content);
        Assert.Equal("Informatik__Algorithmen.csv", export.FileName);
        Assert.Equal("text/csv", export.ContentType);
        Assert.Contains("#deck:Informatik::Algorithmen\n", csv);
        Assert.EndsWith("\"Q\";\"A\";\"graphen kürzeste_wege\"\n", csv);
    }

    [Fact]
    public async Task ExportAsync_WithUnknownDeck_Throws()
    {
        await Assert.ThrowsAsync<FlashcardDeckNotFoundException>(() => _sut.ExportAsync(Guid.NewGuid()));
    }
}
