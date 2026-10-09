namespace StudyHub.Logic.Domain.Contract;

/// <summary>The study time of one study day, in whole minutes.</summary>
/// <param name="ReviewCount">Flashcard answers given that day.</param>
/// <param name="MinutesByCourse">Study time per course; time without a course is only part of <paramref name="Minutes"/>.</param>
public sealed record StudyTimeDay(
    DateOnly Date,
    int Minutes,
    int ReviewCount,
    IReadOnlyDictionary<Guid, int> MinutesByCourse
);
