namespace StudyHub.Shared.Flashcards;

/// <summary>
/// One Anki "Basic" card. <see cref="Front"/>/<see cref="Back"/> may contain simple HTML
/// (<c>&lt;br&gt;</c>, <c>&lt;code&gt;</c>, <c>&lt;b&gt;</c>), since Anki renders card fields as HTML.
/// </summary>
public sealed record FlashcardDto(string Front, string Back, IReadOnlyList<string> Tags)
{
    public const int FrontMaxLength = 2_000;
    public const int BackMaxLength = 10_000;
    public const int TagMaxLength = 50;
    public const int MaxTagsPerCard = 10;
}
