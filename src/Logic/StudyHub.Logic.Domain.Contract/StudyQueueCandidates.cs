using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>The head of each of a deck's three queues.</summary>
/// <param name="FirstLearning">The learning or relearning card due first, however far ahead.</param>
/// <param name="FirstReview">The review card due first, if it is due today.</param>
/// <param name="FirstNew">The new card added first.</param>
public sealed record StudyQueueCandidates(
    Flashcard? FirstLearning,
    Flashcard? FirstReview,
    Flashcard? FirstNew
);
