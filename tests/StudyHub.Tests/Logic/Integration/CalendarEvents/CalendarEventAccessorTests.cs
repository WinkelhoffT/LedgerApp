using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StudyHub.Api;
using StudyHub.Logic.Integration.Calendar;
using StudyHub.Logic.Integration.CalendarEvents;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Dashboard;
using StudyHub.Logic.Integration.Semesters;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Tests.Api;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Integration.CalendarEvents;

/// <summary>Runs the accessors against the real StudyHub.Api, so both sides of the wire contract are covered.</summary>
public class CalendarEventAccessorTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 10, 8);

    private static WebApplicationFactory<Program> CreateFactory() =>
        InMemoryApiFactory
            .Create()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                    services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now))
                )
            );

    private static async Task<CourseDto> CreateCourseAsync(HttpClient client)
    {
        var semester = await new SemesterAccessor(client).CreateAsync(
            new CreateSemesterRequest(
                "Winter 2026/27",
                new DateOnly(2026, 10, 1),
                new DateOnly(2027, 3, 31)
            )
        );
        return await new CourseAccessor(client).CreateAsync(
            new CreateCourseRequest("Algorithms", null, "#2563eb", semester.Id)
        );
    }

    [Fact]
    public async Task CreateUpdateDelete_ShowUpInTheCalendarAndOnTheDashboard()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var course = await CreateCourseAsync(client);
        var events = new CalendarEventAccessor(client);

        var exam = await events.CreateAsync(
            new CreateCalendarEventRequest(
                CalendarEventKind.Exam,
                "Algorithms exam",
                course.Id,
                null,
                Today.AddDays(12),
                new TimeOnly(10, 0),
                120,
                "Audimax"
            )
        );
        var deadline = await events.CreateAsync(
            new CreateCalendarEventRequest(
                CalendarEventKind.Deadline,
                "Sheet 3",
                course.Id,
                null,
                Today,
                null,
                null,
                null
            )
        );
        var updated = await events.UpdateAsync(
            new UpdateCalendarEventRequest(
                deadline.Id,
                CalendarEventKind.Deadline,
                "Sheet 3",
                course.Id,
                null,
                Today,
                new TimeOnly(23, 59),
                null,
                null
            )
        );
        var upcoming = await new DashboardAccessor(client).GetUpcomingEventsAsync();
        var month = await new CalendarAccessor(client).GetMonthAsync(2026, 10);
        await events.DeleteAsync(exam.Id);
        var afterDelete = await new DashboardAccessor(client).GetUpcomingEventsAsync();

        Assert.Equal(
            ("Algorithms", "#2563eb", new TimeOnly(12, 0)),
            (exam.OwnerName, exam.Color, exam.EndTime)
        );
        Assert.Equal(new TimeOnly(23, 59), updated.StartTime);
        Assert.Equal(
            [("Sheet 3", 0), ("Algorithms exam", 12)],
            upcoming.Select(u => (u.Event.Title, u.DaysUntil))
        );
        Assert.Equal(
            CalendarEventKind.Exam,
            Assert.Single(month.Days.Single(d => d.Date == Today.AddDays(12)).Events).Event.Kind
        );
        Assert.Equal("Sheet 3", Assert.Single(afterDelete).Event.Title);
    }

    [Fact]
    public async Task Errors_AreMappedToTheSharedExceptions()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var course = await CreateCourseAsync(client);
        await new CourseAccessor(client).ArchiveAsync(course.Id);
        var events = new CalendarEventAccessor(client);
        var unknownId = Guid.NewGuid();

        var validation = await Assert.ThrowsAsync<CalendarEventValidationException>(() =>
            events.CreateAsync(
                new CreateCalendarEventRequest(
                    CalendarEventKind.Deadline,
                    "Sheet 3",
                    null,
                    null,
                    Today,
                    new TimeOnly(12, 0),
                    30,
                    null
                )
            )
        );
        var notFound = await Assert.ThrowsAsync<CalendarEventNotFoundException>(() =>
            events.DeleteAsync(unknownId)
        );
        var archived = await Assert.ThrowsAsync<CourseArchivedException>(() =>
            events.CreateAsync(
                new CreateCalendarEventRequest(
                    CalendarEventKind.Exam,
                    "Algorithms exam",
                    course.Id,
                    null,
                    Today,
                    null,
                    null,
                    null
                )
            )
        );

        Assert.Equal("A deadline has a due time but no duration.", validation.Message);
        Assert.Equal(unknownId, notFound.CalendarEventId);
        Assert.Equal(course.Id, archived.CourseId);
    }
}
