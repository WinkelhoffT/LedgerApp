using System.Globalization;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Domain;

public sealed class PracticeExamSourceProcessor : IPracticeExamSourceProcessor
{
    public PracticeExamMaterial CreateFromNotes(string courseName, IReadOnlyList<Note> notes)
    {
        var usable = notes
            .Where(note => !string.IsNullOrWhiteSpace(note.Content))
            .OrderBy(note => note.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (usable.Count == 0)
        {
            throw new PracticeExamValidationException(
                "There are no notes with content to write a practice exam from."
            );
        }

        var length = usable.Sum(note => note.Title.Length + note.Content.Length);
        if (length > GeneratePracticeExamRequest.MaxMaterialLength)
        {
            throw new PracticeExamValidationException(
                $"The selected notes have {Format(length)} characters, but at most {Format(GeneratePracticeExamRequest.MaxMaterialLength)} can be used. Deselect some notes."
            );
        }

        var sources = usable
            .Select(
                (note, index) =>
                    new PracticeExamSource(index + 1, note.Title, note.Content, note.Id, null)
            )
            .ToList();
        return new PracticeExamMaterial(courseName, sources, length, OmittedCount: 0);
    }

    public PracticeExamMaterial CreateFromCards(string deckName, IReadOnlyList<Flashcard> cards)
    {
        if (cards.Count == 0)
        {
            throw new PracticeExamValidationException(
                "The deck has no cards to write a practice exam from."
            );
        }

        // OrderBy is stable, so cards that tie keep the order they were given in.
        var ordered = cards
            .OrderByDescending(card => card.Lapses)
            .ThenBy(card => card.EaseFactor)
            .ToList();

        var selected = new List<Flashcard>();
        var length = 0;
        foreach (var card in ordered)
        {
            var cardLength = GetLength(card);
            if (length + cardLength > GeneratePracticeExamRequest.MaxMaterialLength)
            {
                break;
            }

            selected.Add(card);
            length += cardLength;
        }

        var sources = selected
            .Select(
                (card, index) =>
                    new PracticeExamSource(index + 1, string.Empty, FormatCard(card), null, card.Id)
            )
            .ToList();
        return new PracticeExamMaterial(deckName, sources, length, cards.Count - selected.Count);
    }

    private static int GetLength(Flashcard card) =>
        card.Front.Length + card.Back.Length + (card.Tags?.Length ?? 0);

    private static string FormatCard(Flashcard card) =>
        string.IsNullOrWhiteSpace(card.Tags)
            ? $"Front: {card.Front}\nBack: {card.Back}"
            : $"Front: {card.Front}\nBack: {card.Back}\nTags: {card.Tags}";

    private static string Format(int characters) =>
        characters.ToString("N0", CultureInfo.InvariantCulture);
}
