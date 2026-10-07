using StudyHub.Shared.Anki;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>Today's due cards summed over the top-level Anki decks.</summary>
public sealed record AnkiDueCards(
    int NewCount,
    int LearnCount,
    int ReviewCount,
    bool HasCardsToStudy,
    IReadOnlyList<AnkiDeckCountsDto> TopLevelDecks);
