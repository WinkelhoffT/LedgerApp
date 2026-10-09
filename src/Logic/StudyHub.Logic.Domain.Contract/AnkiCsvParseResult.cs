using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <param name="DuplicateMode">From <c>#if matches:</c>; <c>null</c> when the file does not set it.</param>
public sealed record AnkiCsvParseResult(
    ImportDuplicateMode? DuplicateMode,
    IReadOnlyList<AnkiCsvRow> Rows
);
