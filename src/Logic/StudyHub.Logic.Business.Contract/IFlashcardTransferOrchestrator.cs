using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Business.Contract;

/// <summary>Anki CSV import into decks and deck export in the same format.</summary>
public interface IFlashcardTransferOrchestrator
{
    Task<FlashcardImportResultDto> ImportAsync(
        ImportFlashcardsRequest request,
        CancellationToken cancellationToken = default
    );

    Task<FlashcardExportDto> ExportAsync(
        Guid deckId,
        CancellationToken cancellationToken = default
    );
}
