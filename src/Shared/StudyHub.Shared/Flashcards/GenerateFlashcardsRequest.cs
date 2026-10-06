namespace StudyHub.Shared.Flashcards;

public sealed record GenerateFlashcardsRequest(Guid NoteId, int CardCount, string? FocusHint)
{
    public const int MinCardCount = 1;
    public const int MaxCardCount = 50;
    public const int DefaultCardCount = 15;
    public const int FocusHintMaxLength = 500;
}
