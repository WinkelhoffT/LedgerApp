using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Flashcards;

/// <summary>
/// Narrow HTTP access to StudyHub.Api's flashcard endpoints, covering what the Flashcards page calls.
/// </summary>
public interface IFlashcardAccessor
{
    Task<FlashcardSetDto> GenerateAsync(GenerateFlashcardsRequest request, CancellationToken cancellationToken = default);

    Task<FlashcardExportDto> ExportAsync(ExportFlashcardsRequest request, CancellationToken cancellationToken = default);
}
