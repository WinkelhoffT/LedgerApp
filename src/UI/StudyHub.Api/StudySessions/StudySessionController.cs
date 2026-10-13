using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Api.StudySessions;

[ApiController]
[Route("api/study-sessions")]
public sealed class StudySessionController(IStudySessionOrchestrator sessionOrchestrator)
    : ControllerBase
{
    [HttpPost]
    public Task<StudySessionDto> CreateAsync(
        CreateStudySessionRequest request,
        CancellationToken cancellationToken
    ) => sessionOrchestrator.CreateAsync(request, cancellationToken);

    // The route id always wins over whatever Id is present in the request body.
    [HttpPut("{id:guid}")]
    public Task<StudySessionDto> UpdateAsync(
        Guid id,
        UpdateStudySessionRequest request,
        CancellationToken cancellationToken
    ) => sessionOrchestrator.UpdateAsync(request with { Id = id }, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<NoContentResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await sessionOrchestrator.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/completion")]
    public Task<StudySessionDto> CompleteAsync(
        Guid id,
        CompleteStudySessionRequest request,
        CancellationToken cancellationToken
    ) => sessionOrchestrator.CompleteAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}/completion")]
    public Task<StudySessionDto> ResetCompletionAsync(
        Guid id,
        CancellationToken cancellationToken
    ) => sessionOrchestrator.ResetCompletionAsync(id, cancellationToken);
}
