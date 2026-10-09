using System.Net.Http.Json;
using StudyHub.Shared.Analytics;
using StudyHub.Shared.Calendar;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Dashboard;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Dashboard;

public sealed class DashboardAccessor(HttpClient httpClient) : IDashboardAccessor
{
    public async Task<SemesterProgressDto> GetSemesterProgressAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            "api/dashboard/semester-progress",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SemesterProgressDto>(cancellationToken))!;
    }

    public async Task<FlashcardsDueDto> GetFlashcardsDueAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            "api/dashboard/flashcards-due",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FlashcardsDueDto>(cancellationToken))!;
    }

    public async Task<CalendarDayDto> GetSessionsTodayAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            "api/dashboard/sessions-today",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CalendarDayDto>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<UpcomingCalendarEventDto>> GetUpcomingEventsAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            "api/dashboard/upcoming-events",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return (
            await response.Content.ReadFromJsonAsync<IReadOnlyList<UpcomingCalendarEventDto>>(
                cancellationToken
            )
        )!;
    }

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
}
