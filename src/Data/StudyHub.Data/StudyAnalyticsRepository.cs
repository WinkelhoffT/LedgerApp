using Microsoft.EntityFrameworkCore;
using StudyHub.Data.Contract;
using StudyHub.Shared.Analytics;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Data;

public sealed class StudyAnalyticsRepository(ApplicationDbContext dbContext)
    : IStudyAnalyticsRepository
{
    public async Task<IReadOnlyList<StudySession>> GetSessionsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    ) =>
        await dbContext
            .StudySessions.AsNoTracking()
            .Where(s => s.Date >= from && s.Date <= to)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FlashcardReviewActivity>> GetReviewActivitiesAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default
    ) =>
        await (
            from review in dbContext.FlashcardReviews.AsNoTracking()
            join card in dbContext.Flashcards.AsNoTracking() on review.FlashcardId equals card.Id
            join deck in dbContext.FlashcardDecks.AsNoTracking() on card.DeckId equals deck.Id
            where review.ReviewedAt >= fromUtc && review.ReviewedAt < toUtc
            orderby review.ReviewedAt
            select new FlashcardReviewActivity(review.ReviewedAt, deck.CourseId)
        ).ToListAsync(cancellationToken);

    public async Task<
        IReadOnlyList<PracticeExamAttemptActivity>
    > GetSubmittedAttemptActivitiesAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default
    ) =>
        await (
            from attempt in dbContext.PracticeExamAttempts.AsNoTracking()
            join exam in dbContext.PracticeExams.AsNoTracking() on attempt.ExamId equals exam.Id
            join deck in dbContext.FlashcardDecks.AsNoTracking()
                on exam.DeckId equals deck.Id
                into decks
            from deck in decks.DefaultIfEmpty()
            where
                attempt.SubmittedAt != null
                && attempt.SubmittedAt >= fromUtc
                && attempt.StartedAt < toUtc
            orderby attempt.StartedAt
            select new PracticeExamAttemptActivity(
                attempt.StartedAt,
                attempt.SubmittedAt!.Value,
                exam.DurationMinutes,
                exam.CourseId ?? (deck == null ? null : deck.CourseId)
            )
        ).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FlashcardDeckProgressCounts>> GetDeckProgressCountsAsync(
        CancellationToken cancellationToken = default
    ) =>
        await (
            from card in dbContext.Flashcards.AsNoTracking()
            join deck in dbContext.FlashcardDecks.AsNoTracking() on card.DeckId equals deck.Id
            where !deck.IsArchived
            group card by new { deck.Id, deck.CourseId } into g
            select new FlashcardDeckProgressCounts(
                g.Key.Id,
                g.Key.CourseId,
                g.Count(c => c.State == FlashcardState.New),
                g.Count(c =>
                    c.State == FlashcardState.Learning || c.State == FlashcardState.Relearning
                ),
                g.Count(c =>
                    c.State == FlashcardState.Review
                    && c.IntervalDays < FlashcardDeckProgressCounts.MatureIntervalDays
                ),
                g.Count(c =>
                    c.State == FlashcardState.Review
                    && c.IntervalDays >= FlashcardDeckProgressCounts.MatureIntervalDays
                )
            )
        ).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PracticeExamResult>> GetGradedResultsAsync(
        CancellationToken cancellationToken = default
    ) =>
        await (
            from attempt in dbContext.PracticeExamAttempts.AsNoTracking()
            join exam in dbContext.PracticeExams.AsNoTracking() on attempt.ExamId equals exam.Id
            join deck in dbContext.FlashcardDecks.AsNoTracking()
                on exam.DeckId equals deck.Id
                into decks
            from deck in decks.DefaultIfEmpty()
            where attempt.GradedAt != null && attempt.AwardedPoints != null && !exam.IsArchived
            orderby attempt.GradedAt
            select new PracticeExamResult(
                exam.CourseId ?? (deck == null ? null : deck.CourseId),
                attempt.GradedAt!.Value,
                attempt.AwardedPoints!.Value,
                attempt.MaxPoints
            )
        ).ToListAsync(cancellationToken);
}
