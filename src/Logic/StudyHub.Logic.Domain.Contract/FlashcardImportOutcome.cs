using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>What an import changes; nothing is stored yet.</summary>
/// <param name="Decks">Every deck a row was added to, updated in or skipped in.</param>
public sealed record FlashcardImportOutcome(
    IReadOnlyList<FlashcardDeck> CreatedDecks,
    IReadOnlyList<Flashcard> AddedCards,
    IReadOnlyList<Flashcard> UpdatedCards,
    int SkippedDuplicates,
    IReadOnlyList<FlashcardImportFailureDto> Failures,
    IReadOnlyList<ImportedFlashcardDeckDto> Decks
);
