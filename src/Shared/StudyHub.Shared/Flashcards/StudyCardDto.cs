namespace StudyHub.Shared.Flashcards;

/// <summary>The next card of a study session, the deck's remaining counts and the interval behind each answer button.</summary>
public sealed record StudyCardDto(
    Guid CardId,
    Guid DeckId,
    string DeckName,
    string Front,
    string Back,
    IReadOnlyList<string> Tags,
    FlashcardState State,
    FlashcardStudyCountsDto Counts,
    IReadOnlyList<FlashcardIntervalPreviewDto> Intervals);
