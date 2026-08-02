using System.Net.Http.Json;
using StudyHub.Logic.Business.Dashboard;

namespace StudyHub.UI.Dashboard;

/// <summary>
/// Adapter satisfying <see cref="IDashboardManagement"/> over HTTP against StudyHub.Api instead of
/// running the use case in-process. Transport failures (connection errors, timeouts, malformed
/// responses) are intentionally left untranslated and surface via Blazor's default error handling.
/// </summary>
public sealed class DashboardApiClient(HttpClient httpClient) : IDashboardManagement
{
    public async Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/dashboard/semester-progress", cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SemesterProgressDto>(cancellationToken))!;
    }
}
