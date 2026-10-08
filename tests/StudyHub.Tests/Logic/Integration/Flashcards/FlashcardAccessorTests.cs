using System.Text;
using StudyHub.Logic.Integration.Dashboard;
using StudyHub.Logic.Integration.Flashcards;
using StudyHub.Shared.Flashcards;
using StudyHub.Tests.Api;

namespace StudyHub.Tests.Logic.Integration.Flashcards;

/// <summary>Runs the accessors against the real StudyHub.Api, so both sides of the wire contract are covered.</summary>
public class FlashcardAccessorTests
{
    [Fact]
    public async Task StudySession_RoundTripsCardsAndReturnsNullWhenFinished()
    {
        using var factory = InMemoryApiFactory.Create();
        var client = factory.CreateClient();
        var decks = new FlashcardDeckAccessor(client);
        var study = new FlashcardStudyAccessor(client);
        var deck = await decks.CreateAsync(new CreateFlashcardDeckRequest("Algorithmen", null, 20, 200));
        await decks.AddCardsAsync(new AddFlashcardsRequest(deck.Id, [new FlashcardDto("Q", "A", ["graphen"])], null));

        var card = await study.GetNextAsync(deck.Id);
        var next = await study.AnswerAsync(new AnswerFlashcardRequest(card!.CardId, FlashcardRating.Easy));

        Assert.Equal(["graphen"], card.Tags);
        Assert.Equal(new FlashcardIntervalPreviewDto(FlashcardRating.Hard, TimeSpan.FromMinutes(5.5)), card.Intervals[1]);
        Assert.Null(next);
    }

    [Fact]
    public async Task Errors_AreMappedToTheSharedExceptions()
    {
        using var factory = InMemoryApiFactory.Create();
        var client = factory.CreateClient();
        var decks = new FlashcardDeckAccessor(client);
        var deck = await decks.CreateAsync(new CreateFlashcardDeckRequest("Algorithmen", null, 20, 200));

        var duplicate = await Assert.ThrowsAsync<DuplicateFlashcardDeckNameException>(
            () => decks.CreateAsync(new CreateFlashcardDeckRequest("Algorithmen", null, 20, 200)));
        var notFound = await Assert.ThrowsAsync<FlashcardDeckNotFoundException>(() => decks.GetByIdAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<FlashcardValidationException>(() => decks.AddCardsAsync(new AddFlashcardsRequest(deck.Id, [], null)));
        await decks.ArchiveAsync(deck.Id);
        var archived = await Assert.ThrowsAsync<FlashcardDeckArchivedException>(() => new FlashcardStudyAccessor(client).GetNextAsync(deck.Id));

        Assert.Equal("Algorithmen", duplicate.Name);
        Assert.NotEqual(Guid.Empty, notFound.DeckId);
        Assert.Equal(deck.Id, archived.DeckId);
    }

    [Fact]
    public async Task ImportThenExport_RoundTripsTheCardsAndShowsUpOnTheDashboard()
    {
        using var factory = InMemoryApiFactory.Create();
        var client = factory.CreateClient();
        var transfer = new FlashcardTransferAccessor(client);

        var result = await transfer.ImportAsync(new ImportFlashcardsRequest(
            "Netze.txt",
            Encoding.UTF8.GetBytes("#separator:tab\n#html:true\n#tags column:3\nTCP?\tTransport<br>Layer\tnetze\n"),
            null,
            ImportDuplicateMode.KeepBoth));
        var export = await transfer.ExportAsync(result.Decks[0].DeckId);
        var due = await new DashboardAccessor(client).GetFlashcardsDueAsync();

        Assert.Equal(1, result.Added);
        Assert.Equal(0, result.Failed);
        Assert.Equal("Netze.csv", export.FileName);
        Assert.EndsWith("\"TCP?\";\"Transport<br>Layer\";\"netze\"\n", Encoding.UTF8.GetString(export.Content));
        Assert.Equal(1, due.Total.New);
    }

    [Fact]
    public async Task Import_WithFileLevelError_ThrowsImportException()
    {
        using var factory = InMemoryApiFactory.Create();
        var transfer = new FlashcardTransferAccessor(factory.CreateClient());

        var ex = await Assert.ThrowsAsync<FlashcardImportException>(() => transfer.ImportAsync(new ImportFlashcardsRequest(
            "latin1.csv", Encoding.Latin1.GetBytes("Größe;Antwort\n"), null, ImportDuplicateMode.UpdateCurrent)));

        Assert.Contains("UTF-8", ex.Message);
    }
}
