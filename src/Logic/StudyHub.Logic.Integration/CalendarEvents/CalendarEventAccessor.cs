using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Integration.CalendarEvents;

public sealed class CalendarEventAccessor(HttpClient httpClient) : ICalendarEventAccessor
{
    public async Task<CalendarEventDto> CreateAsync(CreateCalendarEventRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/calendar-events", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<CalendarEventDto>(cancellationToken))!;
    }

    public async Task<CalendarEventDto> UpdateAsync(UpdateCalendarEventRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/calendar-events/{request.Id}", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<CalendarEventDto>(cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"api/calendar-events/{id}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        var errorCode = GetString(problemDetails, "errorCode");

        throw errorCode switch
        {
            CalendarEventErrorCodes.CalendarEventValidationFailed => new CalendarEventValidationException(
                problemDetails?.Detail ?? "Calendar event validation failed."),
            CalendarEventErrorCodes.CalendarEventNotFound => new CalendarEventNotFoundException(GetGuid(problemDetails, "calendarEventId")),
            CourseErrorCodes.CourseNotFound => new CourseNotFoundException(GetGuid(problemDetails, "courseId")),
            CourseErrorCodes.CourseArchived => new CourseArchivedException(GetGuid(problemDetails, "courseId")),
            SemesterErrorCodes.SemesterNotFound => new SemesterNotFoundException(GetGuid(problemDetails, "semesterId")),
            SemesterErrorCodes.SemesterArchived => new SemesterArchivedException(GetGuid(problemDetails, "semesterId")),
            _ => new HttpRequestException(
                $"StudyHub.Api returned {(int)response.StatusCode} ({response.StatusCode}): {problemDetails?.Detail}",
                inner: null,
                response.StatusCode),
        };
    }

    private static string? GetString(ProblemDetails? problemDetails, string key)
    {
        if (problemDetails is null
            || !problemDetails.Extensions.TryGetValue(key, out var value)
            || value is not JsonElement { ValueKind: JsonValueKind.String } element)
        {
            return null;
        }

        return element.GetString();
    }

    private static Guid GetGuid(ProblemDetails? problemDetails, string key) =>
        Guid.TryParse(GetString(problemDetails, key), out var guid) ? guid : Guid.Empty;
}
