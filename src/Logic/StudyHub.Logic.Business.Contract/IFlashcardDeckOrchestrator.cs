using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Business.Contract;

public interface IFlashcardDeckOrchestrator
{
    Task<IReadOnlyList<FlashcardDeckDto>> GetAllAsync(bool includeArchived, CancellationToken cancellationToken = default);

    Task<FlashcardDeckDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FlashcardDeckDto> CreateAsync(CreateFlashcardDeckRequest request, CancellationToken cancellationToken = default);

    Task<FlashcardDeckDto> UpdateAsync(UpdateFlashcardDeckRequest request, CancellationToken cancellationToken = default);

    Task<FlashcardDeckDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FlashcardDeckDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
