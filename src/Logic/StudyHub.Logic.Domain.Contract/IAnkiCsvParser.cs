using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Reads Anki's text import format (Anki 2.1.55+): <c>#</c> header lines, delimiter detection,
/// RFC 4180 quoting, comment lines and Anki's HTML rules. Pure: bytes in, rows out.
/// </summary>
public interface IAnkiCsvParser
{
    /// <exception cref="FlashcardImportException">The file is too large, not UTF-8, or has too many rows.</exception>
    AnkiCsvParseResult Parse(byte[] content);
}
