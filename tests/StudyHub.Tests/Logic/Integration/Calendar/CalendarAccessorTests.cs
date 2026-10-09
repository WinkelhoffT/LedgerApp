using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StudyHub.Api;
using StudyHub.Logic.Integration.Calendar;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Dashboard;
using StudyHub.Logic.Integration.Semesters;
using StudyHub.Logic.Integration.StudySessions;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;
using StudyHub.Tests.Api;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Integration.Calendar;

/// <summary>Runs the accessors against the real StudyHub.Api, so both sides of the wire contract are covered.</summary>
public class CalendarAccessorTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 10, 8);

    private static WebApplicationFactory<Program> CreateFactory() =>
        InMemoryApiFactory.Create().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now))));

    private static async Task<CourseDto> CreateCourseAsync(HttpClient client)
    {
        var semester = await new SemesterAccessor(client).CreateAsync(
            new CreateSemesterRequest("Winter 2026/27", new DateOnly(2026, 10, 1), new DateOnly(2027, 3, 31)));
        return await new CourseAccessor(client).CreateAsync(new CreateCourseRequest("Algorithms", null, "#2563eb", semester.Id));
    }

    [Fact]
    public async Task CreateUpdateDelete_ShowUpInTheCalendarViewsAndOnTheDashboard()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var course = await CreateCourseAsync(client);
        var sessions = new StudySessionAccessor(client);
        var calendar = new CalendarAccessor(client);

        var created = await sessions.CreateAsync(
            new CreateStudySessionRequest("Graph review", course.Id, null, Today, new TimeOnly(9, 0), 90, "Library"));
        var month = await calendar.GetMonthAsync(2026, 10);
        var updated = await sessions.UpdateAsync(
            new UpdateStudySessionRequest(created.Id, "Graph review", course.Id, null, Today, new TimeOnly(21, 30), 60, null));
        var week = await calendar.GetWeekAsync(Today);
        var today = await new DashboardAccessor(client).GetSessionsTodayAsync();
        await sessions.DeleteAsync(created.Id);
        var afterDelete = await calendar.GetWeekAsync(Today);

        var monthSession = Assert.Single(month.Days.Single(d => d.Date == Today).Sessions).Session;
        Assert.Equal(("Algorithms", "#2563eb", new TimeOnly(10, 30)), (monthSession.OwnerName, monthSession.Color, monthSession.EndTime));
        Assert.Equal(new TimeOnly(22, 30), updated.EndTime);
        Assert.Equal(41, week.IsoWeek);
        Assert.Equal(23, week.EndHour);
        Assert.Equal(created.Id, Assert.Single(today.Sessions).Session.Id);
        Assert.All(afterDelete.Days, day => Assert.Empty(day.Sessions));
    }

    [Fact]
    public async Task Errors_AreMappedToTheSharedExceptions()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var course = await CreateCourseAsync(client);
        await new CourseAccessor(client).ArchiveAsync(course.Id);
        var sessions = new StudySessionAccessor(client);
        var unknownId = Guid.NewGuid();

        var validation = await Assert.ThrowsAsync<StudySessionValidationException>(
            () => sessions.CreateAsync(new CreateStudySessionRequest("Late review", null, null, Today, new TimeOnly(23, 30), 60, null)));
        var notFound = await Assert.ThrowsAsync<StudySessionNotFoundException>(() => sessions.DeleteAsync(unknownId));
        var archived = await Assert.ThrowsAsync<CourseArchivedException>(
            () => sessions.CreateAsync(new CreateStudySessionRequest("Graph review", course.Id, null, Today, new TimeOnly(9, 0), 60, null)));
        await Assert.ThrowsAsync<SemesterNotFoundException>(
            () => sessions.CreateAsync(new CreateStudySessionRequest("Workshop", null, Guid.NewGuid(), Today, new TimeOnly(9, 0), 60, null)));

        Assert.Equal("A session must end by midnight of the day it starts.", validation.Message);
        Assert.Equal(unknownId, notFound.StudySessionId);
        Assert.Equal(course.Id, archived.CourseId);
    }
}
