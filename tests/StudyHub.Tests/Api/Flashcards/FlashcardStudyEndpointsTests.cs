using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Tests.Api.Flashcards;

public class FlashcardStudyEndpointsTests
{
    private static async Task<Guid> CreateDeckWithCardsAsync(HttpClient client, params string[] fronts)
    {
        var deckResponse = await client.PostAsJsonAsync("api/flashcard-decks", new CreateFlashcardDeckRequest("Algorithmen", null, 20, 200));
        var deck = (await deckResponse.Content.ReadFromJsonAsync<FlashcardDeckDto>())!;

        if (fronts.Length > 0)
        {
            var cards = fronts.Select(front => new FlashcardDto(front, "Antwort", [])).ToList();
            (await client.PostAsJsonAsync($"api/flashcard-decks/{deck.Id}/cards", new AddFlashcardsRequest(deck.Id, cards, null)))
                .EnsureSuccessStatusCode();
        }

        return deck.Id;
    }

    private static async Task<string?> GetErrorCodeAsync(HttpResponseMessage response)
    {
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return problemDetails!.Extensions.TryGetValue("errorCode", out var value) && value is JsonElement element
            ? element.GetString()
            : null;
    }

    [Fact]
    public async Task Next_ReturnsFirstNewCardWithCountsAndIntervals()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var deckId = await CreateDeckWithCardsAsync(client, "Q1", "Q2");

        var card = await client.GetFromJsonAsync<StudyCardDto>($"api/flashcard-study/{deckId}/next");

        Assert.Equal("Q1", card!.Front);
        Assert.Equal(FlashcardState.New, card.State);
        Assert.Equal(new FlashcardStudyCountsDto(2, 0, 0), card.Counts);
        Assert.Equal(
            [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5.5), TimeSpan.FromMinutes(10), TimeSpan.FromDays(4)],
            card.Intervals.Select(i => i.Interval));
    }

    [Fact]
    public async Task Answer_ReturnsNextCardThen204WhenTheDeckIsFinished()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var deckId = await CreateDeckWithCardsAsync(client, "Q1", "Q2");
        var first = await client.GetFromJsonAsync<StudyCardDto>($"api/flashcard-study/{deckId}/next");

        var afterFirst = await client.PostAsJsonAsync($"api/flashcard-study/cards/{first!.CardId}/answer", new AnswerFlashcardRequest(Guid.Empty, FlashcardRating.Easy));
        var second = await afterFirst.Content.ReadFromJsonAsync<StudyCardDto>();
        var afterSecond = await client.PostAsJsonAsync($"api/flashcard-study/cards/{second!.CardId}/answer", new AnswerFlashcardRequest(Guid.Empty, FlashcardRating.Easy));

        Assert.Equal("Q2", second.Front);
        Assert.Equal(new FlashcardStudyCountsDto(1, 0, 0), second.Counts);
        Assert.Equal(HttpStatusCode.NoContent, afterSecond.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync($"api/flashcard-study/{deckId}/next")).StatusCode);
    }

    [Fact]
    public async Task Answer_CardThatIsNoLongerDue_Returns409WithErrorCode()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var deckId = await CreateDeckWithCardsAsync(client, "Q1");
        var card = await client.GetFromJsonAsync<StudyCardDto>($"api/flashcard-study/{deckId}/next");
        await client.PostAsJsonAsync($"api/flashcard-study/cards/{card!.CardId}/answer", new AnswerFlashcardRequest(Guid.Empty, FlashcardRating.Easy));

        var response = await client.PostAsJsonAsync($"api/flashcard-study/cards/{card.CardId}/answer", new AnswerFlashcardRequest(Guid.Empty, FlashcardRating.Good));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(FlashcardErrorCodes.FlashcardNotDue, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task Next_WithEmptyDeck_Returns204()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var deckId = await CreateDeckWithCardsAsync(client);

        var response = await client.GetAsync($"api/flashcard-study/{deckId}/next");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Next_ForUnknownDeck_Returns404WithErrorCode()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"api/flashcard-study/{Guid.NewGuid()}/next");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(FlashcardErrorCodes.FlashcardDeckNotFound, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task DashboardFlashcardsDue_CountsTheNewCardsOfActiveDecks()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var deckId = await CreateDeckWithCardsAsync(client, "Q1", "Q2", "Q3");

        var due = await client.GetFromJsonAsync<FlashcardsDueDto>("api/dashboard/flashcards-due");

        Assert.Equal(new FlashcardStudyCountsDto(3, 0, 0), due!.Total);
        Assert.Equal(deckId, Assert.Single(due.Decks).DeckId);
    }

    [Fact]
    public void Startup_WithUnknownStudyTimeZone_Fails()
    {
        using var factory = InMemoryApiFactory.Create()
            .WithWebHostBuilder(builder => builder.UseSetting("Flashcards:TimeZone", "Mars/Olympus_Mons"));

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }
}
