namespace StudyHub.Shared.Analytics;

/// <summary>One study day of a week chart.</summary>
/// <param name="ReviewCount">Flashcard answers given that day.</param>
public sealed record StudyDayDto(
    DateOnly Date,
    int Minutes,
    int ReviewCount,
    bool IsToday,
    bool IsFuture
);
