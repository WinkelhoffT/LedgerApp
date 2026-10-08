using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>Which card a deck shows next and how many cards are left today, applying the daily limits.</summary>
public interface IStudyQueueProvider
{
    /// <param name="cardCounts">The deck's card counts for today; <c>null</c> when the deck has no cards.</param>
    /// <param name="reviewCounts">The deck's answers today; <c>null</c> when nothing was answered.</param>
    FlashcardStudyCountsDto GetCounts(FlashcardDeck deck, FlashcardDeckCardCounts? cardCounts, FlashcardDeckReviewCounts? reviewCounts);

    /// <summary>The card to study next, or <c>null</c> when the deck is finished for now.</summary>
    Flashcard? SelectNext(FlashcardDeck deck, StudyQueueCandidates candidates, FlashcardStudyCountsDto counts, DateTime now);
}
