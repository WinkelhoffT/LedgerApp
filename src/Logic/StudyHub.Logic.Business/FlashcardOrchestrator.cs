using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Logic.Integration.Ai;
using StudyHub.Shared.Ai;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;

namespace StudyHub.Logic.Business;

public sealed class FlashcardOrchestrator(
    INoteRepository noteRepository,
    ICourseRepository courseRepository,
    ISemesterRepository semesterRepository,
    IFlashcardValidator flashcardValidator,
    IAnkiCsvSerializer ankiCsvSerializer,
    IFlashcardGenerator flashcardGenerator,
    IAiModelCatalog aiModelCatalog
) : IFlashcardOrchestrator
{
    private const string CsvContentType = "text/csv";

    public IReadOnlyList<AiModelDto> GetAvailableModels() => aiModelCatalog.GetModels();

    public async Task<FlashcardSetDto> GenerateAsync(
        GenerateFlashcardsRequest request,
        CancellationToken cancellationToken = default
    )
    {
        flashcardValidator.ValidateGenerationOptions(request.CardCount, request.FocusHint);

        var model = string.IsNullOrWhiteSpace(request.Model) ? aiModelCatalog.DefaultModelId : request.Model.Trim();
        if (!aiModelCatalog.IsAvailable(model))
        {
            throw new FlashcardValidationException($"The model '{model}' is not available.");
        }

        var note =
            await noteRepository.GetByIdAsync(request.NoteId, cancellationToken)
            ?? throw new NoteNotFoundException(request.NoteId);

        if (note.IsArchived)
        {
            throw new NoteArchivedException(note.Id);
        }

        if (string.IsNullOrWhiteSpace(note.Content))
        {
            throw new FlashcardValidationException("The note is empty - write some content before generating flashcards.");
        }

        var parentName = await GetParentNameAsync(note, cancellationToken);

        var generatedCards = await flashcardGenerator.GenerateAsync(
            new FlashcardGenerationInput(model, note.Title, note.Content, request.CardCount, request.FocusHint),
            cancellationToken
        );

        var cards = flashcardValidator.FilterGeneratedCards(generatedCards, request.CardCount);
        if (cards.Count == 0)
        {
            throw new FlashcardGenerationFailedException(
                FlashcardGenerationFailureReason.InvalidResponse,
                "Claude did not return any usable flashcards for this note."
            );
        }

        return new FlashcardSetDto(
            note.Id,
            ankiCsvSerializer.CreateDeckName(parentName, note.Title),
            ankiCsvSerializer.CreateFileName(note.Title),
            model,
            cards
        );
    }

    public Task<FlashcardExportDto> ExportAsync(
        ExportFlashcardsRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var cards = flashcardValidator.ValidateCards(request.Cards);
        var deckName = ankiCsvSerializer.NormalizeDeckName(request.DeckName);
        var fileName = ankiCsvSerializer.CreateFileName(request.FileName);

        var content = ankiCsvSerializer.Serialize(deckName, cards);

        return Task.FromResult(new FlashcardExportDto(fileName, CsvContentType, content));
    }

    private async Task<string?> GetParentNameAsync(Note note, CancellationToken cancellationToken)
    {
        if (note.CourseId is { } courseId)
        {
            var course = await courseRepository.GetByIdAsync(courseId, cancellationToken);
            return course?.Name;
        }

        if (note.SemesterId is { } semesterId)
        {
            var semester = await semesterRepository.GetByIdAsync(semesterId, cancellationToken);
            return semester?.Name;
        }

        return null;
    }
}
