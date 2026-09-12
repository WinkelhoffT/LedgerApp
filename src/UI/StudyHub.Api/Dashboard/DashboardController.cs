using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Dashboard;

namespace StudyHub.Api.Dashboard;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardManagement dashboardManagement) : ControllerBase
{
    [HttpGet("semester-progress")]
    public Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken) =>
        dashboardManagement.GetSemesterProgressAsync(cancellationToken);
}
