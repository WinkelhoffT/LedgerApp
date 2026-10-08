namespace StudyHub.Shared.Flashcards;

/// <summary>Dashboard summary: today's totals over all active decks and the decks with the most due cards.</summary>
public sealed record FlashcardsDueDto(FlashcardStudyCountsDto Total, IReadOnlyList<FlashcardDeckDueDto> Decks)
{
    public const int MaxDecks = 5;
}
