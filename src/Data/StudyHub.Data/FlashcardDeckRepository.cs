using Microsoft.EntityFrameworkCore;
using StudyHub.Data.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Data;

public sealed class FlashcardDeckRepository(ApplicationDbContext dbContext)
    : IFlashcardDeckRepository
{
    public async Task<IReadOnlyList<FlashcardDeck>> GetAllAsync(
        CancellationToken cancellationToken = default
    ) =>
        await dbContext
            .FlashcardDecks.AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

    public Task<FlashcardDeck?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        dbContext
            .FlashcardDecks.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludingId,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedName = name.Trim().ToLower();

        return dbContext
            .FlashcardDecks.AsNoTracking()
            .Where(d => excludingId == null || d.Id != excludingId)
            .AnyAsync(d => d.Name.ToLower() == normalizedName, cancellationToken);
    }

    public async Task AddAsync(FlashcardDeck deck, CancellationToken cancellationToken = default) =>
        await dbContext.FlashcardDecks.AddAsync(deck, cancellationToken);

    public void Update(FlashcardDeck deck) => dbContext.FlashcardDecks.Update(deck);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
