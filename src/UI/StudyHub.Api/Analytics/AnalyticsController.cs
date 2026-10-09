using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract;
using StudyHub.Shared.Analytics;

namespace StudyHub.Api.Analytics;

[ApiController]
[Route("api/analytics")]
public sealed class AnalyticsController(
    IStudyTimeOrchestrator studyTimeOrchestrator,
    ICourseProgressOrchestrator courseProgressOrchestrator
) : ControllerBase
{
    [HttpGet("study-time")]
    public Task<StudyTimeStatisticsDto> GetStudyTimeAsync(CancellationToken cancellationToken) =>
        studyTimeOrchestrator.GetStatisticsAsync(cancellationToken);

    [HttpGet("course-progress")]
    public Task<CourseProgressOverviewDto> GetCourseProgressAsync(
        CancellationToken cancellationToken
    ) => courseProgressOrchestrator.GetOverviewAsync(cancellationToken);
}
