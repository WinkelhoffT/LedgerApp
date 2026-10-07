using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract;
using StudyHub.Shared.Ai;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Api.Flashcards;

[ApiController]
[Route("api/flashcards")]
public sealed class FlashcardController(IFlashcardOrchestrator flashcardOrchestrator) : ControllerBase
{
    [HttpGet("models")]
    public IReadOnlyList<AiModelDto> GetModels() => flashcardOrchestrator.GetAvailableModels();

    // Nothing is persisted: the cards are returned to the caller, which keeps them until export.
    [HttpPost("generate")]
    public Task<FlashcardSetDto> GenerateAsync(
        GenerateFlashcardsRequest request,
        CancellationToken cancellationToken
    ) => flashcardOrchestrator.GenerateAsync(request, cancellationToken);

    [HttpPost("export")]
    public async Task<FileContentResult> ExportAsync(
        ExportFlashcardsRequest request,
        CancellationToken cancellationToken
    )
    {
        var export = await flashcardOrchestrator.ExportAsync(request, cancellationToken);
        return File(export.Content, export.ContentType, export.FileName);
    }
}
