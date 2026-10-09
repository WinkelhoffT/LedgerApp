using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StudyHub.Api;
using StudyHub.Data;
using StudyHub.Logic.Integration.Ai;
using StudyHub.Shared.Ai;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Shared.Semesters;

namespace StudyHub.Tests.Api.Flashcards;

public class FlashcardEndpointsTests
{
    private static WebApplicationFactory<Program> CreateFactory(
        IFlashcardGenerator? generator = null
    )
    {
        var databaseName = Guid.NewGuid().ToString();

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                $"Data Source={Path.Combine(Path.GetTempPath(), $"studyhub-tests-{databaseName}.db")}"
            );

            builder.ConfigureServices(services =>
            {
                // EF Core 10 also registers the provider through IDbContextOptionsConfiguration, so
                // both registrations must go before switching to the InMemory provider.
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName)
                );

                // Never call the real Anthropic API from tests.
                services.PostConfigure<AnthropicOptions>(options => options.ApiKey = null);
                if (generator is not null)
                {
                    services.RemoveAll<IFlashcardGenerator>();
                    services.AddSingleton(generator);
                }
            });
        });
    }

    private static IFlashcardGenerator FakeGenerator(params FlashcardDto[] cards)
    {
        var generator = new Mock<IFlashcardGenerator>();
        generator
            .Setup(g =>
                g.GenerateAsync(It.IsAny<FlashcardGenerationInput>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(cards);
        return generator.Object;
    }

    private static async Task<NoteDto> CreateNoteAsync(
        HttpClient client,
        string content = "# Dijkstra\nNur nicht-negative Kanten."
    )
    {
        var semesterResponse = await client.PostAsJsonAsync(
            "api/semesters",
            new CreateSemesterRequest(
                "Winter 2025/26",
                new DateOnly(2025, 10, 1),
                new DateOnly(2026, 3, 31)
            )
        );
        var semester = await semesterResponse.Content.ReadFromJsonAsync<SemesterDto>();

        var courseResponse = await client.PostAsJsonAsync(
            "api/courses",
            new CreateCourseRequest("Algorithms", null, "#2563eb", semester!.Id)
        );
        var course = await courseResponse.Content.ReadFromJsonAsync<CourseDto>();

        var noteResponse = await client.PostAsJsonAsync(
            "api/notes",
            new CreateNoteRequest("Dijkstra", content, null, course!.Id, null)
        );
        noteResponse.EnsureSuccessStatusCode();
        return (await noteResponse.Content.ReadFromJsonAsync<NoteDto>())!;
    }

    private static async Task<string?> GetProblemValueAsync(
        HttpResponseMessage response,
        string key
    )
    {
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return
            problemDetails!.Extensions.TryGetValue(key, out var value)
            && value is JsonElement element
            ? element.GetString()
            : null;
    }

    [Fact]
    public async Task Generate_ReturnsCardsAndModel()
    {
        using var factory = CreateFactory(
            FakeGenerator(new FlashcardDto("Frage", "Antwort", ["graphen"]))
        );
        using var client = factory.CreateClient();
        var note = await CreateNoteAsync(client);

        var response = await client.PostAsJsonAsync(
            "api/flashcards/generate",
            new GenerateFlashcardsRequest(note.Id, 5, null)
        );

        response.EnsureSuccessStatusCode();
        var set = await response.Content.ReadFromJsonAsync<FlashcardSetDto>();
        Assert.Equal(note.Id, set!.NoteId);
        Assert.Equal("claude-sonnet-5-5", set.Model);
        Assert.Equal("Frage", Assert.Single(set.Cards).Front);
    }

    [Fact]
    public async Task GetModels_ReturnsConfiguredModelsWithDefault()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var models = await client.GetFromJsonAsync<List<AiModelDto>>("api/flashcards/models");

        Assert.Contains(models!, m => m.Id == "claude-sonnet-5-5" && m.IsDefault);
        Assert.Contains(models!, m => m.Id == "claude-opus-5-5" && !m.IsDefault);
    }

    [Fact]
    public async Task Generate_WithUnknownModel_Returns400WithErrorCode()
    {
        using var factory = CreateFactory(FakeGenerator(new FlashcardDto("Frage", "Antwort", [])));
        using var client = factory.CreateClient();
        var note = await CreateNoteAsync(client);

        var response = await client.PostAsJsonAsync(
            "api/flashcards/generate",
            new GenerateFlashcardsRequest(note.Id, 5, null, "not-a-model")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            FlashcardErrorCodes.FlashcardValidationFailed,
            await GetProblemValueAsync(response, "errorCode")
        );
    }

    [Fact]
    public async Task Generate_WithUnknownNote_Returns404WithNoteErrorCode()
    {
        using var factory = CreateFactory(FakeGenerator());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "api/flashcards/generate",
            new GenerateFlashcardsRequest(Guid.NewGuid(), 5, null)
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            NoteErrorCodes.NoteNotFound,
            await GetProblemValueAsync(response, "errorCode")
        );
    }

    [Fact]
    public async Task Generate_WithInvalidCardCount_Returns400WithErrorCode()
    {
        using var factory = CreateFactory(FakeGenerator());
        using var client = factory.CreateClient();
        var note = await CreateNoteAsync(client);

        var response = await client.PostAsJsonAsync(
            "api/flashcards/generate",
            new GenerateFlashcardsRequest(note.Id, 500, null)
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            FlashcardErrorCodes.FlashcardValidationFailed,
            await GetProblemValueAsync(response, "errorCode")
        );
    }

    [Fact]
    public async Task Generate_WhenGenerationFails_Returns502WithReason()
    {
        var generator = new Mock<IFlashcardGenerator>();
        generator
            .Setup(g =>
                g.GenerateAsync(It.IsAny<FlashcardGenerationInput>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(
                new AiGenerationFailedException(AiGenerationFailureReason.Refused, "declined")
            );
        using var factory = CreateFactory(generator.Object);
        using var client = factory.CreateClient();
        var note = await CreateNoteAsync(client);

        var response = await client.PostAsJsonAsync(
            "api/flashcards/generate",
            new GenerateFlashcardsRequest(note.Id, 5, null)
        );

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(
            nameof(AiGenerationFailureReason.Refused),
            await GetProblemValueAsync(response, "reason")
        );
    }

    [Fact]
    public async Task Generate_WithoutApiKey_Returns503AiNotConfigured()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var note = await CreateNoteAsync(client);

        var response = await client.PostAsJsonAsync(
            "api/flashcards/generate",
            new GenerateFlashcardsRequest(note.Id, 5, null)
        );

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(
            AiErrorCodes.AiNotConfigured,
            await GetProblemValueAsync(response, "errorCode")
        );
    }
}
