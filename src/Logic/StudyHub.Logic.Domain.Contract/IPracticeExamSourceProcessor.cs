using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Builds the numbered material a practice exam is generated from, within
/// <see cref="GeneratePracticeExamRequest.MaxMaterialLength"/>.
/// </summary>
public interface IPracticeExamSourceProcessor
{
    /// <summary>One source per note with content, ordered by title. Empty notes are left out.</summary>
    /// <exception cref="PracticeExamValidationException">No note has content, or the notes are over the limit.</exception>
    PracticeExamMaterial CreateFromNotes(string courseName, IReadOnlyList<Note> notes);

    /// <summary>
    /// One source per card. Cards the student struggles with come first (most lapses, then lowest
    /// ease), then the others in the given order; above the limit the rest is left out.
    /// </summary>
    /// <exception cref="PracticeExamValidationException">The deck has no cards.</exception>
    PracticeExamMaterial CreateFromCards(string deckName, IReadOnlyList<Flashcard> cards);
}
