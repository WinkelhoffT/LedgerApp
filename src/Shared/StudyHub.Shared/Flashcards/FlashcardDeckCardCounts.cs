namespace StudyHub.Shared.Flashcards;

/// <summary>Per-deck card counts read from the database for one study day.</summary>
/// <param name="LearningDue">Learning and relearning cards due before the end of the study day.</param>
/// <param name="ReviewDue">Review cards due before the end of the study day.</param>
public sealed record FlashcardDeckCardCounts(
    Guid DeckId,
    int Total,
    int New,
    int LearningDue,
    int ReviewDue
);
