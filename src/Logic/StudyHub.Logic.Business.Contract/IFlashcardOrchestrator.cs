using StudyHub.Shared.Ai;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Business.Contract;

public interface IFlashcardOrchestrator
{
    IReadOnlyList<AiModelDto> GetAvailableModels();

    Task<FlashcardSetDto> GenerateAsync(GenerateFlashcardsRequest request, CancellationToken cancellationToken = default);

    Task<FlashcardExportDto> ExportAsync(ExportFlashcardsRequest request, CancellationToken cancellationToken = default);
}
