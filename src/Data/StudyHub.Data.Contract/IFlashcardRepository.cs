using StudyHub.Shared.Flashcards;

namespace StudyHub.Data.Contract;

/// <summary>Stored cards and their review log.</summary>
public interface IFlashcardRepository
{
    Task<Flashcard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <param name="search">Matches front, back or tags (case-insensitive); <c>null</c> returns every card.</param>
    Task<IReadOnlyList<Flashcard>> GetByDeckIdAsync(
        Guid deckId,
        string? search = null,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Flashcard>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Flashcard>> GetByDeckIdsAsync(
        IReadOnlyCollection<Guid> deckIds,
        CancellationToken cancellationToken = default
    );

    /// <summary>Card counts per deck that has cards; learning and review cards count as due when due before <paramref name="dueBefore"/>.</summary>
    Task<IReadOnlyList<FlashcardDeckCardCounts>> GetCardCountsAsync(
        DateTime dueBefore,
        Guid? deckId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Answers per deck given at or after <paramref name="reviewedSince"/>.</summary>
    Task<IReadOnlyList<FlashcardDeckReviewCounts>> GetReviewCountsAsync(
        DateTime reviewedSince,
        Guid? deckId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>The learning or relearning card that is due first, however far ahead.</summary>
    Task<Flashcard?> GetFirstLearningCardAsync(
        Guid deckId,
        CancellationToken cancellationToken = default
    );

    /// <summary>The review card that is due first, if it is due before <paramref name="dueBefore"/>.</summary>
    Task<Flashcard?> GetFirstReviewCardAsync(
        Guid deckId,
        DateTime dueBefore,
        CancellationToken cancellationToken = default
    );

    /// <summary>The new card that was added first.</summary>
    Task<Flashcard?> GetFirstNewCardAsync(
        Guid deckId,
        CancellationToken cancellationToken = default
    );

    Task AddRangeAsync(
        IReadOnlyCollection<Flashcard> cards,
        CancellationToken cancellationToken = default
    );

    void Update(Flashcard card);

    void Remove(Flashcard card);

    Task AddReviewAsync(FlashcardReview review, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
