namespace StudyHub.Shared.Flashcards;

/// <summary>
/// Freshly generated cards for one note. Nothing is persisted: the UI keeps this in page state and
/// passes <see cref="DeckName"/>/<see cref="FileName"/> back unchanged on export.
/// </summary>
public sealed record FlashcardSetDto(Guid NoteId, string DeckName, string FileName, IReadOnlyList<FlashcardDto> Cards);
