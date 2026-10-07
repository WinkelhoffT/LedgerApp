using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>What the import dialog chose for rows that name no deck and for duplicates.</summary>
/// <param name="Deck">An existing, active deck; <c>null</c> to use a deck named after <paramref name="FileName"/>.</param>
/// <param name="DuplicateMode">Used unless the file sets <c>#if matches:</c>.</param>
public sealed record FlashcardImportTarget(
    FlashcardDeck? Deck,
    string FileName,
    ImportDuplicateMode DuplicateMode
);
