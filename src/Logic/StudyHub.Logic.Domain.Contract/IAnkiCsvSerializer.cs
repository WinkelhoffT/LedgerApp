using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Anki text-import format rules: deck/file naming and the CSV layout (with Anki's <c>#</c> header
/// lines, so no manual field mapping is needed in Anki's import dialog).
/// </summary>
public interface IAnkiCsvSerializer
{
    /// <summary>Builds <c>StudyHub::&lt;parent&gt;::&lt;note title&gt;</c>.</summary>
    string CreateDeckName(string? parentName, string noteTitle);

    /// <summary>Normalizes a deck name received from a client.</summary>
    /// <exception cref="FlashcardValidationException">The deck name is empty or too long.</exception>
    string NormalizeDeckName(string deckName);

    /// <summary>Turns a note title (or a client-supplied file name) into a safe <c>*.csv</c> file name.</summary>
    string CreateFileName(string name);

    /// <summary>Serializes the cards as UTF-8 (without BOM) Anki CSV.</summary>
    byte[] Serialize(string deckName, IReadOnlyList<FlashcardDto> cards);
}
