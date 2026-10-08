using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Api.Flashcards;

[ApiController]
[Route("api/flashcard-decks")]
public sealed class FlashcardDeckController(
    IFlashcardDeckOrchestrator deckOrchestrator,
    IDeckCardOrchestrator deckCardOrchestrator,
    IFlashcardTransferOrchestrator transferOrchestrator
) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<FlashcardDeckDto>> GetAllAsync(
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken
    ) => deckOrchestrator.GetAllAsync(includeArchived, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<FlashcardDeckDto> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        deckOrchestrator.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    public Task<FlashcardDeckDto> CreateAsync(
        CreateFlashcardDeckRequest request,
        CancellationToken cancellationToken
    ) => deckOrchestrator.CreateAsync(request, cancellationToken);

    // The route id always wins over whatever Id is present in the request body.
    [HttpPut("{id:guid}")]
    public Task<FlashcardDeckDto> UpdateAsync(
        Guid id,
        UpdateFlashcardDeckRequest request,
        CancellationToken cancellationToken
    ) => deckOrchestrator.UpdateAsync(request with { Id = id }, cancellationToken);

    [HttpPost("{id:guid}/archive")]
    public Task<FlashcardDeckDto> ArchiveAsync(Guid id, CancellationToken cancellationToken) =>
        deckOrchestrator.ArchiveAsync(id, cancellationToken);

    [HttpPost("{id:guid}/restore")]
    public Task<FlashcardDeckDto> RestoreAsync(Guid id, CancellationToken cancellationToken) =>
        deckOrchestrator.RestoreAsync(id, cancellationToken);

    [HttpGet("{id:guid}/cards")]
    public Task<IReadOnlyList<DeckCardDto>> GetCardsAsync(
        Guid id,
        [FromQuery] string? search,
        CancellationToken cancellationToken
    ) => deckCardOrchestrator.GetCardsAsync(id, search, cancellationToken);

    [HttpPost("{id:guid}/cards")]
    public Task<IReadOnlyList<DeckCardDto>> AddCardsAsync(
        Guid id,
        AddFlashcardsRequest request,
        CancellationToken cancellationToken
    ) => deckCardOrchestrator.AddCardsAsync(request with { DeckId = id }, cancellationToken);

    [HttpPut("cards/{cardId:guid}")]
    public Task<DeckCardDto> UpdateCardAsync(
        Guid cardId,
        UpdateFlashcardRequest request,
        CancellationToken cancellationToken
    ) => deckCardOrchestrator.UpdateCardAsync(request with { Id = cardId }, cancellationToken);

    [HttpDelete("cards/{cardId:guid}")]
    public async Task<NoContentResult> DeleteCardAsync(Guid cardId, CancellationToken cancellationToken)
    {
        await deckCardOrchestrator.DeleteCardAsync(cardId, cancellationToken);
        return NoContent();
    }

    [HttpPost("import")]
    public async Task<FlashcardImportResultDto> ImportAsync(
        IFormFile file,
        [FromForm] Guid? targetDeckId,
        [FromForm] ImportDuplicateMode duplicateMode,
        CancellationToken cancellationToken
    )
    {
        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);

        var request = new ImportFlashcardsRequest(file.FileName, buffer.ToArray(), targetDeckId, duplicateMode);
        return await transferOrchestrator.ImportAsync(request, cancellationToken);
    }

    [HttpGet("{id:guid}/export")]
    public async Task<FileContentResult> ExportAsync(Guid id, CancellationToken cancellationToken)
    {
        var export = await transferOrchestrator.ExportAsync(id, cancellationToken);
        return File(export.Content, export.ContentType, export.FileName);
    }
}
