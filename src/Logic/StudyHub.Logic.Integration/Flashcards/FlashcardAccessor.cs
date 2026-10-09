using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Shared.Ai;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;

namespace StudyHub.Logic.Integration.Flashcards;

public sealed class FlashcardAccessor(HttpClient httpClient) : IFlashcardAccessor
{
    public async Task<IReadOnlyList<AiModelDto>> GetModelsAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync("api/flashcards/models", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (
            await response.Content.ReadFromJsonAsync<IReadOnlyList<AiModelDto>>(cancellationToken)
        )!;
    }

    public async Task<FlashcardSetDto> GenerateAsync(
        GenerateFlashcardsRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/flashcards/generate",
            request,
            cancellationToken
        );
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<FlashcardSetDto>(cancellationToken))!;
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            cancellationToken
        );
        var errorCode = GetString(problemDetails, "errorCode");

        throw errorCode switch
        {
            FlashcardErrorCodes.FlashcardValidationFailed => new FlashcardValidationException(
                problemDetails?.Detail ?? "Flashcard validation failed."
            ),
            FlashcardErrorCodes.FlashcardGenerationFailed => new FlashcardGenerationFailedException(
                Enum.TryParse<FlashcardGenerationFailureReason>(
                    GetString(problemDetails, "reason"),
                    out var reason
                )
                    ? reason
                    : FlashcardGenerationFailureReason.Unknown,
                problemDetails?.Detail ?? "Flashcard generation failed."
            ),
            FlashcardErrorCodes.AiNotConfigured => new AiNotConfiguredException(),
            NoteErrorCodes.NoteNotFound => new NoteNotFoundException(
                GetGuid(problemDetails, "noteId")
            ),
            NoteErrorCodes.NoteArchived => new NoteArchivedException(
                GetGuid(problemDetails, "noteId")
            ),
            _ => new HttpRequestException(
                $"StudyHub.Api returned {(int)response.StatusCode} ({response.StatusCode}): {problemDetails?.Detail}",
                inner: null,
                response.StatusCode
            ),
        };
    }

    private static string? GetString(ProblemDetails? problemDetails, string key)
    {
        if (
            problemDetails is null
            || !problemDetails.Extensions.TryGetValue(key, out var value)
            || value is not JsonElement { ValueKind: JsonValueKind.String } element
        )
        {
            return null;
        }

        return element.GetString();
    }

    private static Guid GetGuid(ProblemDetails? problemDetails, string key)
    {
        if (
            problemDetails is null
            || !problemDetails.Extensions.TryGetValue(key, out var value)
            || value is not JsonElement { ValueKind: JsonValueKind.String } element
            || !element.TryGetGuid(out var guid)
        )
        {
            return Guid.Empty;
        }

        return guid;
    }
}
