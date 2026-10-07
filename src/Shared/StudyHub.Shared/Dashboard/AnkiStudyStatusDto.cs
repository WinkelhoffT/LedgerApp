using StudyHub.Shared.Anki;

namespace StudyHub.Shared.Dashboard;

public sealed record AnkiStudyStatusDto(
    AnkiConnectionStatus Status,
    bool HasCardsToStudy,
    int NewCount,
    int LearnCount,
    int ReviewCount,
    IReadOnlyList<AnkiDeckCountsDto> Decks)
{
    public int TotalCount => NewCount + LearnCount + ReviewCount;

    public static AnkiStudyStatusDto WithoutCounts(AnkiConnectionStatus status) =>
        new(status, HasCardsToStudy: false, NewCount: 0, LearnCount: 0, ReviewCount: 0, Decks: []);
}
