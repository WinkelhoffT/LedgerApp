using Microsoft.EntityFrameworkCore;
using StudyHub.Data.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Data;

public sealed class FlashcardRepository(ApplicationDbContext dbContext) : IFlashcardRepository
{
    public Task<Flashcard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Flashcards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Flashcard>> GetByDeckIdAsync(Guid deckId, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Flashcards
            .AsNoTracking()
            .Where(c => c.DeckId == deckId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Front.ToLower().Contains(term)
                || c.Back.ToLower().Contains(term)
                || (c.Tags != null && c.Tags.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.DueAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Flashcard>> GetByDeckIdsAsync(IReadOnlyCollection<Guid> deckIds, CancellationToken cancellationToken = default)
    {
        if (deckIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Flashcards
            .AsNoTracking()
            .Where(c => deckIds.Contains(c.DeckId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FlashcardDeckCardCounts>> GetCardCountsAsync(
        DateTime dueBefore,
        Guid? deckId = null,
        CancellationToken cancellationToken = default) =>
        await dbContext.Flashcards
            .AsNoTracking()
            .Where(c => deckId == null || c.DeckId == deckId)
            .GroupBy(c => c.DeckId)
            .Select(g => new FlashcardDeckCardCounts(
                g.Key,
                g.Count(),
                g.Count(c => c.State == FlashcardState.New),
                g.Count(c => (c.State == FlashcardState.Learning || c.State == FlashcardState.Relearning) && c.DueAt < dueBefore),
                g.Count(c => c.State == FlashcardState.Review && c.DueAt < dueBefore)))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FlashcardDeckReviewCounts>> GetReviewCountsAsync(
        DateTime reviewedSince,
        Guid? deckId = null,
        CancellationToken cancellationToken = default) =>
        await (
                from review in dbContext.FlashcardReviews.AsNoTracking()
                join card in dbContext.Flashcards.AsNoTracking() on review.FlashcardId equals card.Id
                where review.ReviewedAt >= reviewedSince && (deckId == null || card.DeckId == deckId)
                group review by card.DeckId into g
                select new FlashcardDeckReviewCounts(
                    g.Key,
                    g.Count(r => r.StateBefore == FlashcardState.New),
                    g.Count(r => r.StateBefore == FlashcardState.Review)))
            .ToListAsync(cancellationToken);

    public Task<Flashcard?> GetFirstLearningCardAsync(Guid deckId, CancellationToken cancellationToken = default) =>
        dbContext.Flashcards
            .AsNoTracking()
            .Where(c => c.DeckId == deckId && (c.State == FlashcardState.Learning || c.State == FlashcardState.Relearning))
            .OrderBy(c => c.DueAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Flashcard?> GetFirstReviewCardAsync(Guid deckId, DateTime dueBefore, CancellationToken cancellationToken = default) =>
        dbContext.Flashcards
            .AsNoTracking()
            .Where(c => c.DeckId == deckId && c.State == FlashcardState.Review && c.DueAt < dueBefore)
            .OrderBy(c => c.DueAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Flashcard?> GetFirstNewCardAsync(Guid deckId, CancellationToken cancellationToken = default) =>
        dbContext.Flashcards
            .AsNoTracking()
            .Where(c => c.DeckId == deckId && c.State == FlashcardState.New)
            .OrderBy(c => c.DueAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task AddRangeAsync(IReadOnlyCollection<Flashcard> cards, CancellationToken cancellationToken = default) =>
        dbContext.Flashcards.AddRangeAsync(cards, cancellationToken);

    public void Update(Flashcard card) =>
        dbContext.Flashcards.Update(card);

    public void Remove(Flashcard card) =>
        dbContext.Flashcards.Remove(card);

    public async Task AddReviewAsync(FlashcardReview review, CancellationToken cancellationToken = default) =>
        await dbContext.FlashcardReviews.AddAsync(review, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
