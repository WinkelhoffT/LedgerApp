using System.Net.Http.Json;
using StudyHub.Shared.Dashboard;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Dashboard;

public sealed class DashboardAccessor(HttpClient httpClient) : IDashboardAccessor
{
    public async Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/dashboard/semester-progress", cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SemesterProgressDto>(cancellationToken))!;
    }

    public async Task<FlashcardsDueDto> GetFlashcardsDueAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/dashboard/flashcards-due", cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FlashcardsDueDto>(cancellationToken))!;
    }
}
