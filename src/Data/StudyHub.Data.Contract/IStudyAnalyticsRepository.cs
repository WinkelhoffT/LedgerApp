using StudyHub.Shared.Analytics;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Data.Contract;

/// <summary>
/// Read-model repository for the learning analytics (LAY-9): small projections over study sessions,
/// flashcard reviews, decks and practice exam attempts. It only reads; nothing is changed through it.
/// </summary>
public interface IStudyAnalyticsRepository
{
    /// <summary>Sessions from <paramref name="from"/> to <paramref name="to"/>, both days included, ordered by date and start time.</summary>
    Task<IReadOnlyList<StudySession>> GetSessionsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    );

    /// <summary>Answers given at or after <paramref name="fromUtc"/> and before <paramref name="toUtc"/>, ordered by time.</summary>
    Task<IReadOnlyList<FlashcardReviewActivity>> GetReviewActivitiesAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default
    );

    /// <summary>Submitted attempts that overlap the range: submitted at or after <paramref name="fromUtc"/> and started before <paramref name="toUtc"/>.</summary>
    Task<IReadOnlyList<PracticeExamAttemptActivity>> GetSubmittedAttemptActivitiesAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default
    );

    /// <summary>Card counts of every deck that is not archived and has cards.</summary>
    Task<IReadOnlyList<FlashcardDeckProgressCounts>> GetDeckProgressCountsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Graded attempts at practice exams that are not archived.</summary>
    Task<IReadOnlyList<PracticeExamResult>> GetGradedResultsAsync(
        CancellationToken cancellationToken = default
    );
}
