using System.Net.Http.Json;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Flashcards;

public sealed class FlashcardDeckAccessor(HttpClient httpClient) : IFlashcardDeckAccessor
{
    public async Task<IReadOnlyList<FlashcardDeckDto>> GetAllAsync(bool includeArchived, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/flashcard-decks?includeArchived={includeArchived}", cancellationToken);
        await FlashcardProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<FlashcardDeckDto>>(cancellationToken))!;
    }

    public async Task<FlashcardDeckDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/flashcard-decks/{id}", cancellationToken);
        await FlashcardProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<FlashcardDeckDto>(cancellationToken))!;
    }

    public async Task<FlashcardDeckDto> CreateAsync(CreateFlashcardDeckRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/flashcard-decks", request, cancellationToken);
        await FlashcardProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<FlashcardDeckDto>(cancellationToken))!;
    }

    public async Task<FlashcardDeckDto> UpdateAsync(UpdateFlashcardDeckRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/flashcard-decks/{request.Id}", request, cancellationToken);
        await FlashcardProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<FlashcardDeckDto>(cancellationToken))!;
    }

    public async Task<FlashcardDeckDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/flashcard-decks/{id}/archive", content: null, cancellationToken);
        await FlashcardProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<FlashcardDeckDto>(cancellationToken))!;
    }

    public async Task<FlashcardDeckDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/flashcard-decks/{id}/restore", content: null, cancellationToken);
        await FlashcardProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<FlashcardDeckDto>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<DeckCardDto>> GetCardsAsync(Guid deckId, string? search, CancellationToken cancellationToken = default)
    {
        var query = string.IsNullOrWhiteSpace(search) ? string.Empty : $"?search={Uri.EscapeDataString(search)}";
        using var response = await httpClient.GetAsync($"api/flashcard-decks/{deckId}/cards{query}", cancellationToken);
        await FlashcardProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<DeckCardDto>>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<DeckCardDto>> AddCardsAsync(AddFlashcardsRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync($"api/flashcard-decks/{request.DeckId}/cards", request, cancellationToken);
        await FlashcardProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<DeckCardDto>>(cancellationToken))!;
    }

    public async Task<DeckCardDto> UpdateCardAsync(UpdateFlashcardRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/flashcard-decks/cards/{request.Id}", request, cancellationToken);
        await FlashcardProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<DeckCardDto>(cancellationToken))!;
    }

    public async Task DeleteCardAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"api/flashcard-decks/cards/{cardId}", cancellationToken);
        await FlashcardProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
    }
}
