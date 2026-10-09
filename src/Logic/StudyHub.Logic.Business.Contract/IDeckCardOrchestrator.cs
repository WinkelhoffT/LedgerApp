using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Business.Contract;

/// <summary>Listing, adding (by hand or generated), editing and deleting the cards of a deck.</summary>
public interface IDeckCardOrchestrator
{
    Task<IReadOnlyList<DeckCardDto>> GetCardsAsync(
        Guid deckId,
        string? search,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<DeckCardDto>> AddCardsAsync(
        AddFlashcardsRequest request,
        CancellationToken cancellationToken = default
    );

    Task<DeckCardDto> UpdateCardAsync(
        UpdateFlashcardRequest request,
        CancellationToken cancellationToken = default
    );

    Task DeleteCardAsync(Guid cardId, CancellationToken cancellationToken = default);
}
