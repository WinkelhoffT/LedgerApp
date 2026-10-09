namespace StudyHub.Shared.Flashcards;

public sealed record FlashcardImportResultDto(
    int Added,
    int Updated,
    int SkippedDuplicates,
    IReadOnlyList<FlashcardImportFailureDto> Failures,
    IReadOnlyList<ImportedFlashcardDeckDto> Decks
)
{
    public int Failed => Failures.Count;
}
