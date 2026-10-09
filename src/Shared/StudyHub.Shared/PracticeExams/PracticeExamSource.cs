namespace StudyHub.Shared.PracticeExams;

/// <summary>
/// One numbered piece of material sent to the AI: a note or a flashcard. Exactly one of
/// <see cref="NoteId"/> and <see cref="FlashcardId"/> is set.
/// </summary>
/// <param name="Id">The number the AI refers to in a task's <c>sourceId</c>, starting at 1.</param>
/// <param name="Title">The note's title; empty for a card.</param>
public sealed record PracticeExamSource(
    int Id,
    string Title,
    string Content,
    Guid? NoteId,
    Guid? FlashcardId
);
