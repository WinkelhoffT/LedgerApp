namespace StudyHub.Logic.Domain.Contract;

/// <summary>One data row of an Anki text file, mapped to a Basic card's fields.</summary>
/// <param name="LineNumber">1-based line the row starts on.</param>
/// <param name="Front">Field content as HTML (plain-text files are HTML-escaped).</param>
/// <param name="DeckName">From the deck column or <c>#deck:</c>; <c>null</c> when the file names no deck.</param>
/// <param name="NoteType">From the note type column or <c>#notetype:</c>; <c>null</c> when the file names none.</param>
public sealed record AnkiCsvRow(
    int LineNumber,
    string Front,
    string Back,
    IReadOnlyList<string> Tags,
    string? DeckName,
    string? NoteType);
