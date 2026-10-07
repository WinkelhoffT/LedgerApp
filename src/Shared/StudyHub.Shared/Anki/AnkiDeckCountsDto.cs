namespace StudyHub.Shared.Anki;

/// <summary>
/// Anki's "due today" counts for one deck, as reported by AnkiConnect's <c>getDeckStats</c>.
/// Counts of a parent deck already include its subdecks.
/// </summary>
public sealed record AnkiDeckCountsDto(string DeckName, int NewCount, int LearnCount, int ReviewCount);
