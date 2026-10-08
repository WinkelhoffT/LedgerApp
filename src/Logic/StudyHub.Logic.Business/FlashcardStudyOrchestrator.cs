using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Business;

public sealed class FlashcardStudyOrchestrator(
    IFlashcardDeckRepository deckRepository,
    IFlashcardRepository flashcardRepository,
    IFlashcardReviewProcessor reviewProcessor,
    IStudyQueueProvider studyQueueProvider,
    IStudyDayProvider studyDayProvider
) : IFlashcardStudyOrchestrator
{
    public async Task<StudyCardDto?> GetNextAsync(Guid deckId, CancellationToken cancellationToken = default)
    {
        var deck = await GetActiveDeckAsync(deckId, cancellationToken);
        return await GetNextCardAsync(deck, studyDayProvider.GetCurrent(), cancellationToken);
    }

    public async Task<StudyCardDto?> AnswerAsync(
        AnswerFlashcardRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (!Enum.IsDefined(request.Rating))
        {
            throw new FlashcardValidationException($"'{request.Rating}' is not a valid rating.");
        }

        var card =
            await flashcardRepository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new FlashcardNotFoundException(request.CardId);
        var deck = await GetActiveDeckAsync(card.DeckId, cancellationToken);
        var today = studyDayProvider.GetCurrent();

        var counts = await GetCountsAsync(deck, today, cancellationToken);
        if (!studyQueueProvider.IsDue(deck, card, counts, today))
        {
            throw new FlashcardNotDueException(card.Id);
        }

        var outcome = reviewProcessor.Answer(card, request.Rating, today);

        flashcardRepository.Update(outcome.Card);
        await flashcardRepository.AddReviewAsync(outcome.Review, cancellationToken);
        await flashcardRepository.SaveChangesAsync(cancellationToken);

        return await GetNextCardAsync(deck, today, cancellationToken);
    }

    private async Task<FlashcardDeck> GetActiveDeckAsync(Guid deckId, CancellationToken cancellationToken)
    {
        var deck =
            await deckRepository.GetByIdAsync(deckId, cancellationToken)
            ?? throw new FlashcardDeckNotFoundException(deckId);

        return deck.IsArchived ? throw new FlashcardDeckArchivedException(deck.Id) : deck;
    }

    private async Task<StudyCardDto?> GetNextCardAsync(FlashcardDeck deck, StudyDay today, CancellationToken cancellationToken)
    {
        var counts = await GetCountsAsync(deck, today, cancellationToken);
        var candidates = new StudyQueueCandidates(
            await flashcardRepository.GetFirstLearningCardAsync(deck.Id, cancellationToken),
            await flashcardRepository.GetFirstReviewCardAsync(deck.Id, today.NextStart, cancellationToken),
            await flashcardRepository.GetFirstNewCardAsync(deck.Id, cancellationToken));

        if (studyQueueProvider.SelectNext(deck, candidates, counts, today) is not { } card)
        {
            return null;
        }

        return new StudyCardDto(
            card.Id,
            deck.Id,
            deck.Name,
            card.Front,
            card.Back,
            FlashcardMapper.SplitTags(card.Tags),
            card.State,
            counts,
            reviewProcessor.PreviewIntervals(card, today));
    }

    private async Task<FlashcardStudyCountsDto> GetCountsAsync(FlashcardDeck deck, StudyDay today, CancellationToken cancellationToken)
    {
        var cardCounts = await flashcardRepository.GetCardCountsAsync(today.NextStart, deck.Id, cancellationToken);
        var reviewCounts = await flashcardRepository.GetReviewCountsAsync(today.Start, deck.Id, cancellationToken);

        return studyQueueProvider.GetCounts(deck, cardCounts.FirstOrDefault(), reviewCounts.FirstOrDefault());
    }
}
