using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Matches parsed Anki rows against existing decks and cards: which cards to add, which to update
/// and which rows to skip. A deck named in the file is found by name (any case) or created; the file
/// wins over the dialog, as in Anki.
/// </summary>
public interface IFlashcardImportProcessor
{
    /// <summary>The existing decks the rows go to; their cards are needed to find duplicates.</summary>
    IReadOnlyList<Guid> GetTargetDeckIds(AnkiCsvParseResult file, FlashcardImportTarget target, IReadOnlyList<FlashcardDeck> decks);

    /// <param name="decks">All existing decks.</param>
    /// <param name="existingCards">The cards of the decks from <see cref="GetTargetDeckIds"/>.</param>
    /// <exception cref="FlashcardImportException">No row of the file can be imported.</exception>
    FlashcardImportOutcome Process(
        AnkiCsvParseResult file,
        FlashcardImportTarget target,
        IReadOnlyList<FlashcardDeck> decks,
        IReadOnlyList<Flashcard> existingCards);
}
