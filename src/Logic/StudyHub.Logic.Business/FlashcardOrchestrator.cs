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
    IFlashcardValidator flashcardValidator,
    IFlashcardGenerator flashcardGenerator,
    IAiModelCatalog aiModelCatalog
) : IFlashcardOrchestrator
{
    public IReadOnlyList<AiModelDto> GetAvailableModels() => aiModelCatalog.GetModels();

    public async Task<FlashcardSetDto> GenerateAsync(
        GenerateFlashcardsRequest request,
        CancellationToken cancellationToken = default
    )
    {
        flashcardValidator.ValidateGenerationOptions(request.CardCount, request.FocusHint);

        var model = string.IsNullOrWhiteSpace(request.Model)
            ? aiModelCatalog.DefaultModelId
            : request.Model.Trim();
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
            throw new FlashcardValidationException(
                "The note is empty - write some content before generating flashcards."
            );
        }

        var generatedCards = await flashcardGenerator.GenerateAsync(
            new FlashcardGenerationInput(
                model,
                note.Title,
                note.Content,
                request.CardCount,
                request.FocusHint
            ),
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

        return new FlashcardSetDto(note.Id, model, cards);
    }
}
