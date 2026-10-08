namespace StudyHub.Shared.Flashcards;

/// <summary>
/// A flat deck of Basic cards with its own daily study limits. A deck belongs to at most one of a
/// course or a semester (a course already belongs to a semester), or to neither.
/// </summary>
public sealed record FlashcardDeck(
    Guid Id,
    string Name,
    Guid? CourseId,
    Guid? SemesterId,
    int NewCardsPerDay,
    int ReviewsPerDay,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public const int NameMaxLength = 200;
    public const int DefaultNewCardsPerDay = 20;
    public const int DefaultReviewsPerDay = 200;
    public const int MaxCardsPerDay = 9_999;
}
