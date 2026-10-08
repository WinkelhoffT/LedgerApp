namespace StudyHub.Shared.Flashcards;

/// <summary>The cards still to study today, as Anki shows them: new (blue), learning (red), review (green).</summary>
public sealed record FlashcardStudyCountsDto(int New, int Learning, int Review)
{
    public static FlashcardStudyCountsDto Empty { get; } = new(0, 0, 0);

    public int Total => New + Learning + Review;
}
