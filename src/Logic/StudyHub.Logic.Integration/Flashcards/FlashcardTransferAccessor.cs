using System.Net.Http.Headers;
using System.Net.Http.Json;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Flashcards;

public sealed class FlashcardTransferAccessor(HttpClient httpClient) : IFlashcardTransferAccessor
{
    public async Task<FlashcardImportResultDto> ImportAsync(ImportFlashcardsRequest request, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(request.Content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", request.FileName);
        content.Add(new StringContent(request.DuplicateMode.ToString()), "duplicateMode");

        if (request.TargetDeckId is { } targetDeckId)
        {
            content.Add(new StringContent(targetDeckId.ToString()), "targetDeckId");
        }

        using var response = await httpClient.PostAsync("api/flashcard-decks/import", content, cancellationToken);
        await FlashcardProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<FlashcardImportResultDto>(cancellationToken))!;
    }

    public async Task<FlashcardExportDto> ExportAsync(Guid deckId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/flashcard-decks/{deckId}/export", cancellationToken);
        await FlashcardProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? "flashcards.csv";
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/csv";

        return new FlashcardExportDto(fileName.Trim('"'), contentType, content);
    }
}
