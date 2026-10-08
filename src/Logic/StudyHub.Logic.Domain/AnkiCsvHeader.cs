using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain;

/// <summary>The settings from an Anki text file's <c>#key:value</c> header lines.</summary>
/// <param name="ColumnsValue">Raw <c>#columns:</c> value; split once the delimiter is known.</param>
/// <param name="DeckColumn">1-based, like the other column numbers.</param>
internal sealed record AnkiCsvHeader(
    char? Delimiter = null,
    bool? IsHtml = null,
    IReadOnlyList<string>? Tags = null,
    string? ColumnsValue = null,
    string? DeckName = null,
    string? NoteType = null,
    int? DeckColumn = null,
    int? NoteTypeColumn = null,
    int? TagsColumn = null,
    int? GuidColumn = null,
    ImportDuplicateMode? DuplicateMode = null);
