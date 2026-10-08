using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StudyHub.Api;
using StudyHub.Shared.Calendar;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Semesters;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Api.CalendarEvents;

public class CalendarEventEndpointsTests
{
    // 2026-10-08 00:30 in Berlin, while it is still 2026-10-07 in UTC.
    private static readonly DateTime Now = new(2026, 10, 7, 22, 30, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 10, 8);

    private static WebApplicationFactory<Program> CreateFactory() =>
        InMemoryApiFactory.Create().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now))));

    private static CreateCalendarEventRequest ExamRequest(DateOnly date, int? durationMinutes = 120, Guid? semesterId = null) =>
        new(CalendarEventKind.Exam, "Algorithms exam", null, semesterId, date, new TimeOnly(10, 0), durationMinutes, "Audimax");

    private static async Task<CalendarEventDto> CreateAsync(HttpClient client, CreateCalendarEventRequest request)
    {
        var response = await client.PostAsJsonAsync("api/calendar-events", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CalendarEventDto>())!;
    }

    private static async Task<string?> GetErrorCodeAsync(HttpResponseMessage response)
    {
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return problemDetails!.Extensions.TryGetValue("errorCode", out var value) && value is JsonElement element
            ? element.GetString()
            : null;
    }

    [Fact]
    public async Task Create_ThenShowsUpInTheWeekWithItsLane()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var exam = await CreateAsync(client, ExamRequest(Today));
        var week = await client.GetFromJsonAsync<CalendarWeekDto>("api/calendar/week?date=2026-10-08");

        Assert.Equal(new TimeOnly(12, 0), exam.EndTime);
        var entry = Assert.Single(week!.Days.Single(d => d.Date == Today).Events);
        Assert.Equal((exam.Id, 0, 1), (entry.Event.Id, entry.Lane, entry.LaneCount));
    }

    [Fact]
    public async Task Create_WithInvalidTime_Returns400WithErrorCode()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/calendar-events", ExamRequest(Today, durationMinutes: null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(CalendarEventErrorCodes.CalendarEventValidationFailed, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task Create_WithUnknownSemester_Returns404WithSemesterErrorCode()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/calendar-events", ExamRequest(Today, semesterId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(SemesterErrorCodes.SemesterNotFound, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task Update_UsesTheRouteIdAndDelete_Returns204ThenNotFound()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var exam = await CreateAsync(client, ExamRequest(Today));

        var update = await client.PutAsJsonAsync(
            $"api/calendar-events/{exam.Id}",
            new UpdateCalendarEventRequest(Guid.NewGuid(), CalendarEventKind.Deadline, "Sheet 3", null, null, Today, new TimeOnly(23, 59), null, null));
        var first = await client.DeleteAsync($"api/calendar-events/{exam.Id}");
        var second = await client.DeleteAsync($"api/calendar-events/{exam.Id}");

        update.EnsureSuccessStatusCode();
        var updated = (await update.Content.ReadFromJsonAsync<CalendarEventDto>())!;
        Assert.Equal((exam.Id, CalendarEventKind.Deadline), (updated.Id, updated.Kind));
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal(CalendarEventErrorCodes.CalendarEventNotFound, await GetErrorCodeAsync(second));
    }

    [Fact]
    public async Task GetDashboardUpcomingEvents_ReturnsEventsFromTodayOnWithDaysUntil()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await CreateAsync(client, ExamRequest(Today.AddDays(-1)));
        await CreateAsync(client, ExamRequest(Today.AddDays(12)));
        await CreateAsync(client, new CreateCalendarEventRequest(CalendarEventKind.Deadline, "Sheet 3", null, null, Today, null, null, null));

        var upcoming = await client.GetFromJsonAsync<List<UpcomingCalendarEventDto>>("api/dashboard/upcoming-events");

        Assert.Equal([("Sheet 3", 0), ("Algorithms exam", 12)], upcoming!.Select(u => (u.Event.Title, u.DaysUntil)));
    }
}
