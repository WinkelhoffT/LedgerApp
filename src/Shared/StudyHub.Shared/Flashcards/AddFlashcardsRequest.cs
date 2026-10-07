namespace StudyHub.Shared.Flashcards;

/// <param name="SourceNoteId">The note the cards were generated from, if any.</param>
public sealed record AddFlashcardsRequest(
    Guid DeckId,
    IReadOnlyList<FlashcardDto> Cards,
    Guid? SourceNoteId
)
{
    public const int MaxCardCount = 50;
}
