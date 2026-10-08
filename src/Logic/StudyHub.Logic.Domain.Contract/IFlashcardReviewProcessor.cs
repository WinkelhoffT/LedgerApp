using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>Anki's SM-2 scheduler with Anki's default deck options.</summary>
public interface IFlashcardReviewProcessor
{
    /// <summary>Applies <paramref name="rating"/> at <see cref="StudyDay.Now"/>.</summary>
    FlashcardReviewOutcome Answer(Flashcard card, FlashcardRating rating, StudyDay today);

    /// <summary>How long until the card comes back for each rating, as shown on the answer buttons.</summary>
    IReadOnlyList<FlashcardIntervalPreviewDto> PreviewIntervals(Flashcard card, StudyDay today);
}
