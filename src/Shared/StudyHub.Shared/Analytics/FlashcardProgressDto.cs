namespace StudyHub.Shared.Analytics;

/// <summary>Cards in Anki's four buckets.</summary>
/// <param name="Learning">Cards in learning or relearning.</param>
/// <param name="Young">Review cards with an interval under <see cref="FlashcardDeckProgressCounts.MatureIntervalDays"/> days.</param>
/// <param name="Mature">Review cards with an interval of <see cref="FlashcardDeckProgressCounts.MatureIntervalDays"/> days or more.</param>
/// <param name="LearnedPercent">Young and mature cards in percent of all cards; <c>null</c> without cards.</param>
public sealed record FlashcardProgressDto(
    int New,
    int Learning,
    int Young,
    int Mature,
    int Total,
    int? LearnedPercent
);
