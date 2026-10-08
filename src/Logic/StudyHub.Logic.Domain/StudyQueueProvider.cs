using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain;

public sealed class StudyQueueProvider : IStudyQueueProvider
{
    // Anki's default "learn ahead limit": a learning card due this soon may be shown early once
    // nothing else is left, instead of making the user wait.
    private static readonly TimeSpan LearnAheadLimit = TimeSpan.FromMinutes(20);

    public FlashcardStudyCountsDto GetCounts(
        FlashcardDeck deck,
        FlashcardDeckCardCounts? cardCounts,
        FlashcardDeckReviewCounts? reviewCounts)
    {
        if (deck.IsArchived || cardCounts is null)
        {
            return FlashcardStudyCountsDto.Empty;
        }

        var newLeft = Math.Max(0, deck.NewCardsPerDay - (reviewCounts?.NewStudied ?? 0));
        var reviewsLeft = Math.Max(0, deck.ReviewsPerDay - (reviewCounts?.ReviewsDone ?? 0));

        return new FlashcardStudyCountsDto(
            New: Math.Min(cardCounts.New, newLeft),
            Learning: cardCounts.LearningDue,
            Review: Math.Min(cardCounts.ReviewDue, reviewsLeft));
    }

    // Anki's order: learning cards that are due now, then reviews, then new cards; a learning card
    // due within the learn-ahead limit only comes early when nothing else is left.
    public Flashcard? SelectNext(
        FlashcardDeck deck,
        StudyQueueCandidates candidates,
        FlashcardStudyCountsDto counts,
        DateTime now)
    {
        if (deck.IsArchived)
        {
            return null;
        }

        if (candidates.FirstLearning is { } dueLearning && dueLearning.DueAt <= now)
        {
            return dueLearning;
        }

        if (counts.Review > 0 && candidates.FirstReview is { } review)
        {
            return review;
        }

        if (counts.New > 0 && candidates.FirstNew is { } newCard)
        {
            return newCard;
        }

        return candidates.FirstLearning is { } learning && learning.DueAt <= now + LearnAheadLimit
            ? learning
            : null;
    }
}
