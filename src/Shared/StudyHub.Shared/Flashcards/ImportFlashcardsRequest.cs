namespace StudyHub.Shared.Flashcards;

/// <param name="TargetDeckId">Deck for rows that name no deck; <c>null</c> creates (or reuses) a deck named after the file.</param>
/// <param name="DuplicateMode">Used unless the file sets <c>#if matches:</c>.</param>
public sealed record ImportFlashcardsRequest(string FileName, byte[] Content, Guid? TargetDeckId, ImportDuplicateMode DuplicateMode)
{
    public const int MaxFileSizeBytes = 5 * 1024 * 1024;
    public const int MaxRows = 5_000;
}
