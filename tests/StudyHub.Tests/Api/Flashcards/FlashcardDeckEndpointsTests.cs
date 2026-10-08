using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Semesters;

namespace StudyHub.Tests.Api.Flashcards;

public class FlashcardDeckEndpointsTests
{
    private static async Task<FlashcardDeckDto> CreateDeckAsync(HttpClient client, string name = "Algorithmen")
    {
        var response = await client.PostAsJsonAsync("api/flashcard-decks", new CreateFlashcardDeckRequest(name, null, null, 20, 200));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FlashcardDeckDto>())!;
    }

    private static async Task<IReadOnlyList<DeckCardDto>> AddCardsAsync(HttpClient client, Guid deckId, params FlashcardDto[] cards)
    {
        var response = await client.PostAsJsonAsync($"api/flashcard-decks/{deckId}/cards", new AddFlashcardsRequest(deckId, cards, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<DeckCardDto>>())!;
    }

    private static async Task<string?> GetErrorCodeAsync(HttpResponseMessage response)
    {
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return problemDetails!.Extensions.TryGetValue("errorCode", out var value) && value is JsonElement element
            ? element.GetString()
            : null;
    }

    private static MultipartFormDataContent ImportForm(string csv, string fileName, Guid? targetDeckId, string duplicateMode)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", fileName);
        if (targetDeckId is { } id)
        {
            form.Add(new StringContent(id.ToString()), "targetDeckId");
        }

        form.Add(new StringContent(duplicateMode), "duplicateMode");
        return form;
    }

    [Fact]
    public async Task CreateDeckAndAddCards_ThenGetAll_ReturnsDeckWithNewCardsDueToday()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var deck = await CreateDeckAsync(client);

        await AddCardsAsync(client, deck.Id, new FlashcardDto("Q1", "A1", ["graphen"]), new FlashcardDto("Q2", "A2", []));
        var decks = await client.GetFromJsonAsync<List<FlashcardDeckDto>>("api/flashcard-decks");

        var listed = Assert.Single(decks!);
        Assert.Equal(2, listed.CardCount);
        Assert.Equal(new FlashcardStudyCountsDto(2, 0, 0), listed.DueCounts);
    }

    [Fact]
    public async Task Create_WithDuplicateName_Returns409WithErrorCode()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        await CreateDeckAsync(client);

        var response = await client.PostAsJsonAsync("api/flashcard-decks", new CreateFlashcardDeckRequest("algorithmen", null, null, 20, 200));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(FlashcardErrorCodes.DuplicateFlashcardDeckName, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task GetById_WithUnknownId_Returns404WithErrorCode()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"api/flashcard-decks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(FlashcardErrorCodes.FlashcardDeckNotFound, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task Archive_ThenAddCards_Returns409AndListsDeckOnlyWithArchived()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var deck = await CreateDeckAsync(client);

        (await client.PostAsync($"api/flashcard-decks/{deck.Id}/archive", null)).EnsureSuccessStatusCode();
        var response = await client.PostAsJsonAsync($"api/flashcard-decks/{deck.Id}/cards", new AddFlashcardsRequest(deck.Id, [new FlashcardDto("Q", "A", [])], null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(FlashcardErrorCodes.FlashcardDeckArchived, await GetErrorCodeAsync(response));
        Assert.Empty((await client.GetFromJsonAsync<List<FlashcardDeckDto>>("api/flashcard-decks"))!);
        Assert.Single((await client.GetFromJsonAsync<List<FlashcardDeckDto>>("api/flashcard-decks?includeArchived=true"))!);
    }

    [Fact]
    public async Task UpdateCard_ThenSearch_ReturnsEditedCard()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var deck = await CreateDeckAsync(client);
        var cards = await AddCardsAsync(client, deck.Id, new FlashcardDto("Q1", "A1", []), new FlashcardDto("Q2", "A2", []));

        var update = await client.PutAsJsonAsync(
            $"api/flashcard-decks/cards/{cards[0].Id}",
            new UpdateFlashcardRequest(Guid.Empty, new FlashcardDto("Was ist Dijkstra?", "Kürzeste Wege", ["graphen"])));
        update.EnsureSuccessStatusCode();
        var found = await client.GetFromJsonAsync<List<DeckCardDto>>($"api/flashcard-decks/{deck.Id}/cards?search=dijkstra");

        var card = Assert.Single(found!);
        Assert.Equal(cards[0].Id, card.Id);
        Assert.Equal(["graphen"], card.Tags);
    }

    [Fact]
    public async Task DeleteCard_Returns204AndRemovesCard()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var deck = await CreateDeckAsync(client);
        var cards = await AddCardsAsync(client, deck.Id, new FlashcardDto("Q1", "A1", []));

        var response = await client.DeleteAsync($"api/flashcard-decks/cards/{cards[0].Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<DeckCardDto>>($"api/flashcard-decks/{deck.Id}/cards"))!);
    }

    [Fact]
    public async Task UpdateCard_WithUnknownId_Returns404WithErrorCode()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"api/flashcard-decks/cards/{Guid.NewGuid()}",
            new UpdateFlashcardRequest(Guid.Empty, new FlashcardDto("Q", "A", [])));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(FlashcardErrorCodes.FlashcardNotFound, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task Import_Multipart_AddsCardsAndCreatesDecksNamedInTheFile()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var deck = await CreateDeckAsync(client);
        await AddCardsAsync(client, deck.Id, new FlashcardDto("Dijkstra?", "alt", []));
        var csv = "#separator:tab\n#html:true\n#deck column:3\nDijkstra?\tneu\t\nHeap?\tBaum\t\nTCP?\tTransport\tNetze\n";

        var response = await client.PostAsync("api/flashcard-decks/import", ImportForm(csv, "export.txt", deck.Id, "KeepCurrent"));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FlashcardImportResultDto>();
        Assert.Equal(2, result!.Added);
        Assert.Equal(0, result.Updated);
        Assert.Equal(1, result.SkippedDuplicates);
        Assert.Contains(result.Decks, d => d.Name == "Netze" && d.IsNew);
        var decks = await client.GetFromJsonAsync<List<FlashcardDeckDto>>("api/flashcard-decks");
        Assert.Equal([2, 1], decks!.OrderBy(d => d.Name).Select(d => d.CardCount));
    }

    [Fact]
    public async Task Import_WithoutTargetDeck_CreatesDeckNamedAfterTheFile()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("api/flashcard-decks/import", ImportForm("Q;A\n", "Betriebssysteme.csv", null, "UpdateCurrent"));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FlashcardImportResultDto>();
        Assert.Equal("Betriebssysteme", Assert.Single(result!.Decks).Name);
    }

    [Fact]
    public async Task Import_WithNoUsableRow_Returns400WithErrorCode()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("api/flashcard-decks/import", ImportForm("#notetype:Cloze\n{{c1::x}};\n", "x.txt", null, "UpdateCurrent"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(FlashcardErrorCodes.FlashcardImportFailed, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task Export_ReturnsAnkiCsvAttachment()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var deck = await CreateDeckAsync(client, "Informatik::Algorithmen");
        await AddCardsAsync(client, deck.Id, new FlashcardDto("Größe?", "Antwort", ["graphen"]));

        var response = await client.GetAsync($"api/flashcard-decks/{deck.Id}/export");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Informatik__Algorithmen.csv", response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName);
        var csv = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
        Assert.Contains("#deck:Informatik::Algorithmen\n", csv);
        Assert.Contains("\"Größe?\";\"Antwort\";\"graphen\"", csv);
    }

    [Fact]
    public async Task Create_WithSemester_ReturnsDeckLinkedToTheSemester()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var semesterResponse = await client.PostAsJsonAsync(
            "api/semesters",
            new CreateSemesterRequest("Programmierworkshop", new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 18)));
        var semester = (await semesterResponse.Content.ReadFromJsonAsync<SemesterDto>())!;

        var response = await client.PostAsJsonAsync("api/flashcard-decks", new CreateFlashcardDeckRequest("Workshop", null, semester.Id, 20, 200));

        response.EnsureSuccessStatusCode();
        var deck = await response.Content.ReadFromJsonAsync<FlashcardDeckDto>();
        Assert.Equal(semester.Id, deck!.SemesterId);
        Assert.Null(deck.CourseId);
    }

    [Fact]
    public async Task Create_WithUnknownSemester_Returns404WithSemesterErrorCode()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/flashcard-decks", new CreateFlashcardDeckRequest("Workshop", null, Guid.NewGuid(), 20, 200));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(SemesterErrorCodes.SemesterNotFound, await GetErrorCodeAsync(response));
    }
}
