using System.Globalization;
using System.Net.Http.Json;
using StudyHub.Shared.Calendar;

namespace StudyHub.Logic.Integration.Calendar;

public sealed class CalendarAccessor(HttpClient httpClient) : ICalendarAccessor
{
    public async Task<CalendarMonthDto> GetMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            $"api/calendar/month?year={year}&month={month}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CalendarMonthDto>(cancellationToken))!;
    }

    public async Task<CalendarWeekDto> GetWeekAsync(
        DateOnly date,
        CancellationToken cancellationToken = default
    )
    {
        var query = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        using var response = await httpClient.GetAsync(
            $"api/calendar/week?date={query}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CalendarWeekDto>(cancellationToken))!;
    }
}
