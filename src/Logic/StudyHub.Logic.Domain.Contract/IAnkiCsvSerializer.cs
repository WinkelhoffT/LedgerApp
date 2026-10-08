using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Anki text-import format rules for export: file naming and the CSV layout (with Anki's <c>#</c>
/// header lines, so no manual field mapping is needed in Anki's import dialog).
/// </summary>
public interface IAnkiCsvSerializer
{
    /// <summary>Turns a deck name into a safe <c>*.csv</c> file name.</summary>
    string CreateFileName(string name);

    /// <summary>Serializes the cards as UTF-8 (without BOM) Anki CSV.</summary>
    byte[] Serialize(string deckName, IReadOnlyList<FlashcardDto> cards);
}
