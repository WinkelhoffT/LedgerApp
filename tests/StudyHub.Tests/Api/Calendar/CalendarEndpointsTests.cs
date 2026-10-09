using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StudyHub.Api;
using StudyHub.Shared.Calendar;
using StudyHub.Shared.StudySessions;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Api.Calendar;

public class CalendarEndpointsTests
{
    // 2026-10-08 00:30 in Berlin, while it is still 2026-10-07 in UTC.
    private static readonly DateTime Now = new(2026, 10, 7, 22, 30, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 10, 8);

    private static WebApplicationFactory<Program> CreateFactory() =>
        InMemoryApiFactory.Create().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now))));

    private static async Task CreateSessionAsync(HttpClient client, string title, DateOnly date, int hour, int durationMinutes = 60)
    {
        var response = await client.PostAsJsonAsync(
            "api/study-sessions",
            new CreateStudySessionRequest(title, null, null, date, new TimeOnly(hour, 0), durationMinutes, null));
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetMonth_ReturnsTheWholeWeeksWithTodayAndSessions()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await CreateSessionAsync(client, "Graph review", Today, 9);

        var month = await client.GetFromJsonAsync<CalendarMonthDto>("api/calendar/month?year=2026&month=10");

        Assert.Equal(Today, month!.Today);
        Assert.Equal(35, month.Days.Count);
        Assert.Equal("Graph review", Assert.Single(month.Days.Single(d => d.Date == Today).Sessions).Session.Title);
    }

    [Theory]
    [InlineData("api/calendar/month?year=2026&month=13")]
    [InlineData("api/calendar/month?year=2026&month=0")]
    [InlineData("api/calendar/month?year=9999&month=12")]
    [InlineData("api/calendar/month?month=10")]
    [InlineData("api/calendar/week?date=9999-12-31")]
    [InlineData("api/calendar/week")]
    public async Task Get_WithInvalidQuery_Returns400(string url)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetWeek_ReturnsTheIsoWeekWithOverlapLanes()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await CreateSessionAsync(client, "A", Today, 9, durationMinutes: 90);
        await CreateSessionAsync(client, "B", Today, 10);

        var week = await client.GetFromJsonAsync<CalendarWeekDto>("api/calendar/week?date=2026-10-11");

        Assert.Equal(new DateOnly(2026, 10, 5), week!.Start);
        Assert.Equal(new DateOnly(2026, 10, 11), week.End);
        Assert.Equal(41, week.IsoWeek);
        Assert.Equal(7, week.StartHour);
        Assert.Equal(22, week.EndHour);
        Assert.Equal(7, week.Days.Count);
        Assert.Equal(
            [("A", 0, 2), ("B", 1, 2)],
            week.Days.Single(d => d.Date == Today).Sessions.Select(s => (s.Session.Title, s.Lane, s.LaneCount)));
    }

    [Fact]
    public async Task GetDashboardSessionsToday_ReturnsOnlyTodaysSessions()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await CreateSessionAsync(client, "Today", Today, 9);
        await CreateSessionAsync(client, "Yesterday", Today.AddDays(-1), 9);

        var day = await client.GetFromJsonAsync<CalendarDayDto>("api/dashboard/sessions-today");

        Assert.Equal(Today, day!.Date);
        Assert.Equal("Today", Assert.Single(day.Sessions).Session.Title);
    }
}
