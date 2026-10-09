using StudyHub.Data.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Business;

/// <summary>
/// Loads a course's notes or a deck's cards, checks that they may be used, and hands them to
/// <see cref="IPracticeExamSourceProcessor"/>. Keeps the lookups out of the generation orchestrator.
/// </summary>
public sealed class PracticeExamMaterialProvider(
    ICourseRepository courseRepository,
    INoteRepository noteRepository,
    IFlashcardDeckRepository deckRepository,
    IFlashcardRepository flashcardRepository,
    IPracticeExamSourceProcessor sourceProcessor
) : IPracticeExamMaterialProvider
{
    public Task<PracticeExamMaterial> GetMaterialAsync(
        GeneratePracticeExamRequest request,
        CancellationToken cancellationToken = default
    ) =>
        request.SourceKind switch
        {
            PracticeExamSourceKind.Course => GetCourseMaterialAsync(request, cancellationToken),
            PracticeExamSourceKind.Deck => GetDeckMaterialAsync(request, cancellationToken),
            _ => throw new PracticeExamValidationException("Choose a course or a deck."),
        };

    private async Task<PracticeExamMaterial> GetCourseMaterialAsync(
        GeneratePracticeExamRequest request,
        CancellationToken cancellationToken
    )
    {
        var courseId =
            request.CourseId ?? throw new PracticeExamValidationException("Choose a course.");
        var course =
            await courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new CourseNotFoundException(courseId);
        if (course.IsArchived)
        {
            throw new CourseArchivedException(course.Id);
        }

        var notes = (await noteRepository.GetByCourseIdAsync(course.Id, cancellationToken))
            .Where(note => !note.IsArchived)
            .ToList();

        if (request.NoteIds is { } noteIds)
        {
            if (noteIds.Count == 0)
            {
                throw new PracticeExamValidationException("Select at least one note.");
            }

            var notesById = notes.ToDictionary(note => note.Id);
            notes = noteIds
                .Distinct()
                .Select(id =>
                    notesById.GetValueOrDefault(id)
                    ?? throw new PracticeExamValidationException(
                        "A selected note does not belong to the course or is archived."
                    )
                )
                .ToList();

            if (notes.FirstOrDefault(note => string.IsNullOrWhiteSpace(note.Content)) is { } empty)
            {
                throw new PracticeExamValidationException($"The note '{empty.Title}' is empty.");
            }
        }

        return sourceProcessor.CreateFromNotes(course.Name, notes);
    }

    private async Task<PracticeExamMaterial> GetDeckMaterialAsync(
        GeneratePracticeExamRequest request,
        CancellationToken cancellationToken
    )
    {
        var deckId = request.DeckId ?? throw new PracticeExamValidationException("Choose a deck.");
        var deck =
            await deckRepository.GetByIdAsync(deckId, cancellationToken)
            ?? throw new FlashcardDeckNotFoundException(deckId);
        if (deck.IsArchived)
        {
            throw new FlashcardDeckArchivedException(deck.Id);
        }

        var cards = await flashcardRepository.GetByDeckIdAsync(
            deck.Id,
            cancellationToken: cancellationToken
        );
        return sourceProcessor.CreateFromCards(deck.Name, cards);
    }
}
