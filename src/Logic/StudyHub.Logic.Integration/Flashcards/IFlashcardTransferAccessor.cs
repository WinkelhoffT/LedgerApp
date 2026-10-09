using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Flashcards;

/// <summary>Narrow HTTP access to StudyHub.Api's Anki CSV import and deck export.</summary>
public interface IFlashcardTransferAccessor
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
