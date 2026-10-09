namespace StudyHub.Shared.PracticeExams;

/// <summary>The note or card a task is based on. Exactly one of the two ids is set.</summary>
/// <param name="Title">The note's title or the card's front.</param>
/// <param name="DeckId">The card's deck; <c>null</c> for a note.</param>
public sealed record PracticeExamSourceReferenceDto(
    Guid? NoteId,
    Guid? FlashcardId,
    Guid? DeckId,
    string Title
);
