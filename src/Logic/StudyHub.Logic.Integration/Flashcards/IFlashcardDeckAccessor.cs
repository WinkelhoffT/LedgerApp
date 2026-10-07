using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Flashcards;

/// <summary>
/// Narrow HTTP access to StudyHub.Api's deck and card endpoints, covering what the deck overview,
/// the deck page and the generator's "Save to deck" call.
/// </summary>
public interface IFlashcardDeckAccessor
{
    Task<IReadOnlyList<FlashcardDeckDto>> GetAllAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default
    );

    Task<FlashcardDeckDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FlashcardDeckDto> CreateAsync(
        CreateFlashcardDeckRequest request,
        CancellationToken cancellationToken = default
    );

    Task<FlashcardDeckDto> UpdateAsync(
        UpdateFlashcardDeckRequest request,
        CancellationToken cancellationToken = default
    );

    Task<FlashcardDeckDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FlashcardDeckDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default);

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
