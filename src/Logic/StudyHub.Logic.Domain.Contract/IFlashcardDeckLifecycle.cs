using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Domain rules for creating and changing a <see cref="FlashcardDeck"/>. Decks are immutable
/// records, so every operation returns a new instance instead of mutating the given one.
/// </summary>
public interface IFlashcardDeckLifecycle
{
    /// <exception cref="FlashcardValidationException">Name, owner or a daily limit breaks a deck rule.</exception>
    FlashcardDeck Create(string name, Guid? courseId, Guid? semesterId, int newCardsPerDay, int reviewsPerDay);

    /// <exception cref="FlashcardDeckArchivedException">The deck is archived.</exception>
    /// <exception cref="FlashcardValidationException">Name, owner or a daily limit breaks a deck rule.</exception>
    FlashcardDeck Update(FlashcardDeck deck, string name, Guid? courseId, Guid? semesterId, int newCardsPerDay, int reviewsPerDay);

    FlashcardDeck Archive(FlashcardDeck deck);

    FlashcardDeck Restore(FlashcardDeck deck);

    /// <summary>
    /// Brings a deck name into its stored form: one line, whitespace runs collapsed to a single space,
    /// since the name ends up in Anki's single-line <c>#deck:</c> header on export.
    /// </summary>
    /// <exception cref="FlashcardValidationException">The name is empty or too long.</exception>
    string NormalizeName(string name);
}
