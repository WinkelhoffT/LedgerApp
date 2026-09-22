using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Business.Dashboard;
using StudyHub.Shared.Dashboard;

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
}
