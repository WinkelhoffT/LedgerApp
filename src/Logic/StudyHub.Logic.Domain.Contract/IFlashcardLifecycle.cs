using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>Domain rules for creating and editing stored cards.</summary>
public interface IFlashcardLifecycle
{
    /// <summary>
    /// Creates new cards in the given order, which becomes their order in the deck's new-card queue.
    /// </summary>
    /// <exception cref="FlashcardValidationException">A card breaks a card rule.</exception>
    IReadOnlyList<Flashcard> Create(Guid deckId, IReadOnlyList<FlashcardDto> cards, Guid? sourceNoteId);

    /// <summary>Replaces front, back and tags; the learning progress stays.</summary>
    /// <exception cref="FlashcardValidationException">The new content breaks a card rule.</exception>
    Flashcard UpdateContent(Flashcard card, FlashcardDto content);
}
