using StudyHub.Shared.Analytics;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>What study time is built from: completed sessions, flashcard answers and submitted practice exam attempts.</summary>
/// <param name="Sessions">Sessions in the period; those that are not done are ignored.</param>
public sealed record StudyActivities(
    IReadOnlyList<StudySession> Sessions,
    IReadOnlyList<FlashcardReviewActivity> Reviews,
    IReadOnlyList<PracticeExamAttemptActivity> Attempts
);
