using StudyHub.Shared.Flashcards;

namespace StudyHub.Data.Contract;

public interface IFlashcardDeckRepository
{
    Task<IReadOnlyList<FlashcardDeck>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<FlashcardDeck?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludingId,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(FlashcardDeck deck, CancellationToken cancellationToken = default);

    void Update(FlashcardDeck deck);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
