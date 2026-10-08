using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;

namespace StudyHub.Logic.Business;

public sealed class DeckCardOrchestrator(
    IFlashcardDeckRepository deckRepository,
    IFlashcardRepository flashcardRepository,
    INoteRepository noteRepository,
    IFlashcardValidator flashcardValidator,
    IFlashcardLifecycle flashcardLifecycle,
    IStudyDayProvider studyDayProvider
) : IDeckCardOrchestrator
{
    public async Task<IReadOnlyList<DeckCardDto>> GetCardsAsync(
        Guid deckId,
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        await GetExistingDeckAsync(deckId, cancellationToken);

        var cards = await flashcardRepository.GetByDeckIdAsync(deckId, search, cancellationToken);
        return cards.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<DeckCardDto>> AddCardsAsync(
        AddFlashcardsRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var deck = await GetExistingDeckAsync(request.DeckId, cancellationToken);
        EnsureNotArchived(deck);

        if (request.SourceNoteId is { } noteId)
        {
            _ = await noteRepository.GetByIdAsync(noteId, cancellationToken) ?? throw new NoteNotFoundException(noteId);
        }

        var contents = flashcardValidator.ValidateCards(request.Cards);
        var cards = flashcardLifecycle.Create(deck.Id, contents, request.SourceNoteId);

        await flashcardRepository.AddRangeAsync(cards, cancellationToken);
        await flashcardRepository.SaveChangesAsync(cancellationToken);

        return cards.Select(ToDto).ToList();
    }

    public async Task<DeckCardDto> UpdateCardAsync(
        UpdateFlashcardRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var card = await GetExistingCardAsync(request.Id, cancellationToken);
        EnsureNotArchived(await GetExistingDeckAsync(card.DeckId, cancellationToken));

        var updated = flashcardLifecycle.UpdateContent(card, request.Card);

        flashcardRepository.Update(updated);
        await flashcardRepository.SaveChangesAsync(cancellationToken);

        return ToDto(updated);
    }

    public async Task DeleteCardAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        var card = await GetExistingCardAsync(cardId, cancellationToken);
        EnsureNotArchived(await GetExistingDeckAsync(card.DeckId, cancellationToken));

        flashcardRepository.Remove(card);
        await flashcardRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<FlashcardDeck> GetExistingDeckAsync(Guid id, CancellationToken cancellationToken) =>
        await deckRepository.GetByIdAsync(id, cancellationToken) ?? throw new FlashcardDeckNotFoundException(id);

    private async Task<Flashcard> GetExistingCardAsync(Guid id, CancellationToken cancellationToken) =>
        await flashcardRepository.GetByIdAsync(id, cancellationToken) ?? throw new FlashcardNotFoundException(id);

    private static void EnsureNotArchived(FlashcardDeck deck)
    {
        if (deck.IsArchived)
        {
            throw new FlashcardDeckArchivedException(deck.Id);
        }
    }

    private DeckCardDto ToDto(Flashcard card) =>
        FlashcardMapper.ToDeckCardDto(card, studyDayProvider.GetDate(card.DueAt));
}
