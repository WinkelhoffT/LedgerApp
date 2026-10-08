using System.Net;
using System.Net.Http.Json;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Flashcards;

public sealed class FlashcardStudyAccessor(HttpClient httpClient) : IFlashcardStudyAccessor
{
    public async Task<StudyCardDto?> GetNextAsync(Guid deckId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/flashcard-study/{deckId}/next", cancellationToken);
        return await ReadCardAsync(response, cancellationToken);
    }

    public async Task<StudyCardDto?> AnswerAsync(AnswerFlashcardRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync($"api/flashcard-study/cards/{request.CardId}/answer", request, cancellationToken);
        return await ReadCardAsync(response, cancellationToken);
    }

    private static async Task<StudyCardDto?> ReadCardAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await FlashcardProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);

        return response.StatusCode == HttpStatusCode.NoContent
            ? null
            : await response.Content.ReadFromJsonAsync<StudyCardDto>(cancellationToken);
    }
}
