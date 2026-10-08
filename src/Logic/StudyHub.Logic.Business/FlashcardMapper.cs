using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Business;

/// <summary>Maps stored cards to the DTOs and content shapes the flashcard orchestrators return.</summary>
internal static class FlashcardMapper
{
    public static IReadOnlyList<string> SplitTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags) ? [] : tags.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    public static FlashcardDto ToContent(Flashcard card) =>
        new(card.Front, card.Back, SplitTags(card.Tags));

    /// <param name="dueDate">The study day the card is due on; ignored for new cards.</param>
    public static DeckCardDto ToDeckCardDto(Flashcard card, DateOnly dueDate) =>
        new(
            card.Id,
            card.DeckId,
            card.Front,
            card.Back,
            SplitTags(card.Tags),
            card.State,
            card.State == FlashcardState.New ? null : dueDate,
            card.IntervalDays,
            card.Reps,
            card.Lapses,
            card.SourceNoteId,
            card.CreatedAt,
            card.UpdatedAt);

    public static FlashcardDeckDto ToDeckDto(FlashcardDeck deck, int cardCount, FlashcardStudyCountsDto dueCounts) =>
        new(
            deck.Id,
            deck.Name,
            deck.CourseId,
            deck.NewCardsPerDay,
            deck.ReviewsPerDay,
            deck.IsArchived,
            cardCount,
            dueCounts,
            deck.CreatedAt,
            deck.UpdatedAt);
}
