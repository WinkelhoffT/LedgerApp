using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Business;

public sealed class FlashcardTransferOrchestrator(
    IFlashcardDeckRepository deckRepository,
    IFlashcardRepository flashcardRepository,
    IAnkiCsvParser ankiCsvParser,
    IFlashcardImportProcessor importProcessor,
    IAnkiCsvSerializer ankiCsvSerializer
) : IFlashcardTransferOrchestrator
{
    private const string CsvContentType = "text/csv";

    public async Task<FlashcardImportResultDto> ImportAsync(
        ImportFlashcardsRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var file = ankiCsvParser.Parse(request.Content);
        var targetDeck = request.TargetDeckId is { } deckId
            ? await GetActiveDeckAsync(deckId, cancellationToken)
            : null;

        var target = new FlashcardImportTarget(targetDeck, request.FileName, request.DuplicateMode);
        var decks = await deckRepository.GetAllAsync(cancellationToken);
        var existingCards = await flashcardRepository.GetByDeckIdsAsync(
            importProcessor.GetTargetDeckIds(file, target, decks),
            cancellationToken
        );

        var outcome = importProcessor.Process(file, target, decks, existingCards);

        foreach (var deck in outcome.CreatedDecks)
        {
            await deckRepository.AddAsync(deck, cancellationToken);
        }

        await flashcardRepository.AddRangeAsync(outcome.AddedCards, cancellationToken);
        foreach (var card in outcome.UpdatedCards)
        {
            flashcardRepository.Update(card);
        }

        // Decks and cards share one unit of work, so this single save stores the whole file or nothing.
        await flashcardRepository.SaveChangesAsync(cancellationToken);

        return new FlashcardImportResultDto(
            outcome.AddedCards.Count,
            outcome.UpdatedCards.Count,
            outcome.SkippedDuplicates,
            outcome.Failures,
            outcome.Decks
        );
    }

    public async Task<FlashcardExportDto> ExportAsync(
        Guid deckId,
        CancellationToken cancellationToken = default
    )
    {
        var deck =
            await deckRepository.GetByIdAsync(deckId, cancellationToken)
            ?? throw new FlashcardDeckNotFoundException(deckId);
        var cards = await flashcardRepository.GetByDeckIdAsync(
            deckId,
            cancellationToken: cancellationToken
        );

        var content = ankiCsvSerializer.Serialize(
            deck.Name,
            cards.Select(FlashcardMapper.ToContent).ToList()
        );

        return new FlashcardExportDto(
            ankiCsvSerializer.CreateFileName(deck.Name),
            CsvContentType,
            content
        );
    }

    private async Task<FlashcardDeck> GetActiveDeckAsync(
        Guid deckId,
        CancellationToken cancellationToken
    )
    {
        var deck =
            await deckRepository.GetByIdAsync(deckId, cancellationToken)
            ?? throw new FlashcardDeckNotFoundException(deckId);

        return deck.IsArchived ? throw new FlashcardDeckArchivedException(deck.Id) : deck;
    }
}
