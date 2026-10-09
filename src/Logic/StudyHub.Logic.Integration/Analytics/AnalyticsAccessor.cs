using System.Net.Http.Json;
using StudyHub.Shared.Analytics;

namespace StudyHub.Logic.Integration.Analytics;

public sealed class AnalyticsAccessor(HttpClient httpClient) : IAnalyticsAccessor
{
    public async Task<StudyTimeStatisticsDto> GetStudyTimeAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            "api/analytics/study-time",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return (
            await response.Content.ReadFromJsonAsync<StudyTimeStatisticsDto>(cancellationToken)
        )!;
    }

    public async Task<CourseProgressOverviewDto> GetCourseProgressAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            "api/analytics/course-progress",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return (
            await response.Content.ReadFromJsonAsync<CourseProgressOverviewDto>(cancellationToken)
        )!;
    }
}
