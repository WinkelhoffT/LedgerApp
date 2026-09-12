using System.Net.Http.Json;
using StudyHub.Logic.Business.Dashboard;

namespace StudyHub.UI.Dashboard;

public sealed class DashboardAccessor(HttpClient httpClient) : IDashboardAccessor
{
    public async Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/dashboard/semester-progress", cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SemesterProgressDto>(cancellationToken))!;
    }
}
