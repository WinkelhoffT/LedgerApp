using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Api.Flashcards;

/// <summary>Study sessions; <c>204 No Content</c> means the deck is finished for now.</summary>
[ApiController]
[Route("api/flashcard-study")]
public sealed class FlashcardStudyController(IFlashcardStudyOrchestrator studyOrchestrator) : ControllerBase
{
    [HttpGet("{deckId:guid}/next")]
    public async Task<ActionResult<StudyCardDto>> GetNextAsync(Guid deckId, CancellationToken cancellationToken) =>
        ToResult(await studyOrchestrator.GetNextAsync(deckId, cancellationToken));

    // The route card id always wins over whatever CardId is present in the request body.
    [HttpPost("cards/{cardId:guid}/answer")]
    public async Task<ActionResult<StudyCardDto>> AnswerAsync(
        Guid cardId,
        AnswerFlashcardRequest request,
        CancellationToken cancellationToken
    ) => ToResult(await studyOrchestrator.AnswerAsync(request with { CardId = cardId }, cancellationToken));

    private ActionResult<StudyCardDto> ToResult(StudyCardDto? card) =>
        card is null ? NoContent() : card;
}
