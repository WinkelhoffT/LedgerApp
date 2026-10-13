using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StudyHub.Api;
using StudyHub.Logic.Integration.Analytics;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Dashboard;
using StudyHub.Logic.Integration.Semesters;
using StudyHub.Logic.Integration.StudySessions;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;
using StudyHub.Tests.Api;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Integration.Analytics;

/// <summary>Runs the accessors against the real StudyHub.Api, so both sides of the wire contract are covered.</summary>
public class AnalyticsAccessorTests
{
    // Thursday, 12:00 in Berlin.
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

    private static CreateStudySessionRequest SessionRequest(Guid? courseId, DateOnly date) =>
        new("Graph review", courseId, null, date, new TimeOnly(9, 0), 60, null);

    [Fact]
    public async Task MarkingASessionAsDone_ShowsUpInTheStatisticsAndUndoingRemovesIt()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var course = await CreateCourseAsync(client);
        var sessions = new StudySessionAccessor(client);
        var analytics = new AnalyticsAccessor(client);
        var session = await sessions.CreateAsync(SessionRequest(course.Id, Today));

        var completed = await sessions.CompleteAsync(
            session.Id,
            new CompleteStudySessionRequest(75)
        );
        var studyTime = await analytics.GetStudyTimeAsync();
        var dashboardStudyTime = await new DashboardAccessor(client).GetStudyTimeAsync();
        var progress = await analytics.GetCourseProgressAsync();
        var reset = await sessions.ResetCompletionAsync(session.Id);
        var studyTimeAfterUndo = await analytics.GetStudyTimeAsync();

        Assert.True(completed.IsCompleted);
        Assert.Equal(75, completed.ActualDurationMinutes);
        Assert.Equal(Today, studyTime.Today);
        Assert.Equal(75, studyTime.ThisWeekMinutes);
        Assert.Equal(75, studyTime.Days.Single(d => d.IsToday).Minutes);
        Assert.Equal((1, 1), (studyTime.SessionsDoneThisWeek, studyTime.SessionsPlannedThisWeek));
        Assert.Equal(1, studyTime.CurrentStreakDays);
        Assert.Equal(studyTime.ThisWeekMinutes, dashboardStudyTime.ThisWeekMinutes);
        Assert.Equal(75, Assert.Single(progress.Courses).StudyMinutes);
        Assert.False(reset.IsCompleted);
        Assert.False(studyTimeAfterUndo.HasStudyTime);
    }

    [Fact]
    public async Task CompleteAsync_AFutureSession_ThrowsTheValidationException()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var sessions = new StudySessionAccessor(client);
        var session = await sessions.CreateAsync(SessionRequest(null, Today.AddDays(1)));

        await Assert.ThrowsAsync<StudySessionValidationException>(() =>
            sessions.CompleteAsync(session.Id, new CompleteStudySessionRequest(60))
        );
    }

    [Fact]
    public async Task ResetCompletionAsync_OfAnUnknownSession_ThrowsTheNotFoundException()
    {
        using var factory = CreateFactory();
        var id = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<StudySessionNotFoundException>(() =>
            new StudySessionAccessor(factory.CreateClient()).ResetCompletionAsync(id)
        );

        Assert.Equal(id, ex.StudySessionId);
    }
}
