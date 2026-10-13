namespace StudyHub.Shared.Analytics;

/// <summary>Card counts of a deck that is not archived and has cards, in Anki's four buckets.</summary>
/// <param name="Learning">Cards in learning or relearning.</param>
/// <param name="Young">Review cards with an interval under <see cref="MatureIntervalDays"/> days.</param>
/// <param name="Mature">Review cards with an interval of <see cref="MatureIntervalDays"/> days or more.</param>
public sealed record FlashcardDeckProgressCounts(
    Guid DeckId,
    Guid? CourseId,
    int New,
    int Learning,
    int Young,
    int Mature
)
{
    /// <summary>As in Anki, a review card counts as mature from this interval on.</summary>
    public const int MatureIntervalDays = 21;
}
