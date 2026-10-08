using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract;
using StudyHub.Shared.Dashboard;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Api.Dashboard;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardOrchestrator dashboardOrchestrator)
    : ControllerBase
{
    [HttpGet("semester-progress")]
    public Task<SemesterProgressDto> GetSemesterProgressAsync(
        CancellationToken cancellationToken
    ) => dashboardOrchestrator.GetSemesterProgressAsync(cancellationToken);

    [HttpGet("flashcards-due")]
    public Task<FlashcardsDueDto> GetFlashcardsDueAsync(CancellationToken cancellationToken) =>
        dashboardOrchestrator.GetFlashcardsDueAsync(cancellationToken);
}
