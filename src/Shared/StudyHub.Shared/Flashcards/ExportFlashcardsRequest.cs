namespace StudyHub.Shared.Flashcards;

public sealed record ExportFlashcardsRequest(string DeckName, string FileName, IReadOnlyList<FlashcardDto> Cards)
{
    public const int DeckNameMaxLength = 300;
}
