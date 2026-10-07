using System.Net;
using Microsoft.Extensions.Options;
using StudyHub.Logic.Integration.Anki;
using StudyHub.Shared.Anki;
using StudyHub.Shared.Configuration;

namespace StudyHub.Tests.Logic.Integration.Anki;

public class AnkiConnectAccessorTests
{
    private const string DeckNamesAndIds = """
        { "result": { "Informatik": 1, "Informatik::Algorithmen": 2 }, "error": null }
        """;

    // getDeckStats reports only the leaf name, so full names must come from deckNamesAndIds.
    private const string DeckStats = """
        {
          "result": {
            "1": { "deck_id": 1, "name": "Informatik", "new_count": 10, "learn_count": 2, "review_count": 30, "total_in_deck": 500 },
            "2": { "deck_id": 2, "name": "Algorithmen", "new_count": 4, "learn_count": 1, "review_count": 10, "total_in_deck": 120 }
          },
          "error": null
        }
        """;

    private readonly FakeAnkiConnectHandler _handler = new();

    private AnkiConnectAccessor CreateSut(string? apiKey = null)
    {
        var options = new AnkiConnectOptions { Enabled = true, ApiKey = apiKey };
        var httpClient = new HttpClient(_handler) { BaseAddress = options.BaseAddress };
        return new AnkiConnectAccessor(httpClient, Options.Create(options));
    }

    [Fact]
    public async Task GetDeckCountsAsync_ParsesDeckStats_UsingFullDeckNames()
    {
        _handler.RespondTo("deckNamesAndIds", DeckNamesAndIds);
        _handler.RespondTo("getDeckStats", DeckStats);

        var result = await CreateSut().GetDeckCountsAsync();

        Assert.Equal(
            [
                new AnkiDeckCountsDto("Informatik", NewCount: 10, LearnCount: 2, ReviewCount: 30),
                new AnkiDeckCountsDto("Informatik::Algorithmen", NewCount: 4, LearnCount: 1, ReviewCount: 10),
            ],
            result.OrderBy(deck => deck.DeckName));
    }

    [Fact]
    public async Task GetDeckCountsAsync_SendsVersion6Envelope_WithAllDeckNames()
    {
        _handler.RespondTo("deckNamesAndIds", DeckNamesAndIds);
        _handler.RespondTo("getDeckStats", DeckStats);

        await CreateSut().GetDeckCountsAsync();

        Assert.Equal(2, _handler.Requests.Count);
        Assert.All(_handler.Requests, request => Assert.Equal(6, request.GetProperty("version").GetInt32()));
        Assert.All(_handler.Requests, request => Assert.False(request.TryGetProperty("key", out _)));

        var deckStatsRequest = _handler.Requests[1];
        Assert.Equal("getDeckStats", deckStatsRequest.GetProperty("action").GetString());
        Assert.Equal(
            ["Informatik", "Informatik::Algorithmen"],
            deckStatsRequest.GetProperty("params").GetProperty("decks").EnumerateArray().Select(deck => deck.GetString()));
    }

    [Fact]
    public async Task GetDeckCountsAsync_SendsContentLength_BecauseAnkiConnectCannotReadChunkedBodies()
    {
        _handler.RespondTo("deckNamesAndIds", DeckNamesAndIds);
        _handler.RespondTo("getDeckStats", DeckStats);

        await CreateSut().GetDeckCountsAsync();

        Assert.All(_handler.ContentLengths, length => Assert.NotNull(length));
    }

    [Fact]
    public async Task GetDeckCountsAsync_WithApiKey_SendsKey()
    {
        _handler.RespondTo("deckNamesAndIds", DeckNamesAndIds);
        _handler.RespondTo("getDeckStats", DeckStats);

        await CreateSut(apiKey: "secret").GetDeckCountsAsync();

        Assert.All(_handler.Requests, request => Assert.Equal("secret", request.GetProperty("key").GetString()));
    }

    [Fact]
    public async Task GetDeckCountsAsync_WithNoDecks_SkipsDeckStats()
    {
        _handler.RespondTo("deckNamesAndIds", """{ "result": {}, "error": null }""");

        var result = await CreateSut().GetDeckCountsAsync();

        Assert.Empty(result);
        Assert.Single(_handler.Requests);
    }

    [Fact]
    public async Task GetDeckCountsAsync_WhenAnkiConnectReportsError_Throws()
    {
        _handler.RespondTo("deckNamesAndIds", """{ "result": null, "error": "valid api key must be provided" }""");

        var exception = await Assert.ThrowsAsync<AnkiConnectUnavailableException>(() => CreateSut().GetDeckCountsAsync());

        Assert.Contains("valid api key must be provided", exception.Message);
    }

    public static TheoryData<Exception> ConnectionFailures => new()
    {
        new HttpRequestException("Connection refused"),
        new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"),
    };

    [Theory]
    [MemberData(nameof(ConnectionFailures))]
    public async Task GetDeckCountsAsync_WhenAnkiIsUnreachable_Throws(Exception failure)
    {
        _handler.ThrowOn("deckNamesAndIds", failure);

        await Assert.ThrowsAsync<AnkiConnectUnavailableException>(() => CreateSut().GetDeckCountsAsync());
    }

    [Fact]
    public async Task GetDeckCountsAsync_WhenResponseIsNotAnkiConnectJson_Throws()
    {
        _handler.RespondTo("deckNamesAndIds", "<html>not anki</html>");

        await Assert.ThrowsAsync<AnkiConnectUnavailableException>(() => CreateSut().GetDeckCountsAsync());
    }

    [Fact]
    public async Task GetDeckCountsAsync_WhenHttpStatusIsNotSuccess_Throws()
    {
        _handler.RespondTo("deckNamesAndIds", "{}", HttpStatusCode.Forbidden);

        await Assert.ThrowsAsync<AnkiConnectUnavailableException>(() => CreateSut().GetDeckCountsAsync());
    }

    [Fact]
    public async Task GetDeckCountsAsync_WhenCallerCancels_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateSut().GetDeckCountsAsync(cancellation.Token));
    }
}
