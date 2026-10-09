namespace StudyHub.Shared.Flashcards;

/// <param name="Model">Model id from <c>GET api/flashcards/models</c>; <c>null</c> uses the configured default.</param>
public sealed record GenerateFlashcardsRequest(
    Guid NoteId,
    int CardCount,
    string? FocusHint,
    string? Model = null
)
{
    public const int MinCardCount = 1;
    public const int MaxCardCount = 50;
    public const int DefaultCardCount = 15;
    public const int FocusHintMaxLength = 500;
}
