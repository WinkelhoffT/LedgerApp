namespace StudyHub.Shared.Flashcards;

/// <summary>
/// Freshly generated cards for one note. They are not stored yet: the UI keeps them in page state
/// until the user saves them to a deck.
/// </summary>
/// <param name="Model">Id of the model that generated the cards.</param>
public sealed record FlashcardSetDto(Guid NoteId, string Model, IReadOnlyList<FlashcardDto> Cards);
