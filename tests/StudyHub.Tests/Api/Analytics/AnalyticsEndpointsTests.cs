using System.Net.Http.Json;
using StudyHub.Shared.Analytics;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Tests.Api.Analytics;

public class AnalyticsEndpointsTests
{
    // Yesterday in the calendar time zone is today or yesterday as a study day, so it is always in
    // the statistics, whatever time the tests run at.
    private static DateOnly CalendarYesterday() =>
        DateOnly
            .FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")
                )
            )
            .AddDays(-1);

    [Fact]
    public async Task GetStudyTime_WithoutData_ReturnsEmptySeries()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();

        var statistics = await client.GetFromJsonAsync<StudyTimeStatisticsDto>(
            "api/analytics/study-time"
        );

        Assert.False(statistics!.HasStudyTime);
        Assert.Equal(7, statistics.Days.Count);
        Assert.Equal(StudyTimeStatisticsDto.WeekCount, statistics.Weeks.Count);
        Assert.Equal(StudyTimeStatisticsDto.HeatmapWeekCount * 7, statistics.Heatmap.Count);
    }

    [Fact]
    public async Task GetStudyTime_CountsACompletedSession()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var yesterday = CalendarYesterday();
        var created = await client.PostAsJsonAsync(
            "api/study-sessions",
            new CreateStudySessionRequest(
                "Graph review",
                null,
                null,
                yesterday,
                new TimeOnly(12, 0),
                60,
                null
            )
        );
        var session = (await created.Content.ReadFromJsonAsync<StudySessionDto>())!;
        (
            await client.PutAsJsonAsync(
                $"api/study-sessions/{session.Id}/completion",
                new CompleteStudySessionRequest(75)
            )
        ).EnsureSuccessStatusCode();

        var statistics = await client.GetFromJsonAsync<StudyTimeStatisticsDto>(
            "api/analytics/study-time"
        );

        Assert.True(statistics!.HasStudyTime);
        Assert.Equal(1, statistics.LongestStreakDays);
        Assert.Equal(75, statistics.Heatmap.Single(d => d.Date == yesterday).Minutes);
    }

    [Fact]
    public async Task GetCourseProgress_WithoutActiveSemester_ReturnsEmptyState()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();

        var overview = await client.GetFromJsonAsync<CourseProgressOverviewDto>(
            "api/analytics/course-progress"
        );

        Assert.False(overview!.HasActiveSemester);
        Assert.Empty(overview.Courses);
    }

    [Fact]
    public async Task GetCourseProgress_ListsTheCoursesOfTheActiveSemester()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var semesterResponse = await client.PostAsJsonAsync(
            "api/semesters",
            new CreateSemesterRequest("Winter 2026/27", today.AddDays(-30), today.AddDays(30))
        );
        var semester = (await semesterResponse.Content.ReadFromJsonAsync<SemesterDto>())!;
        (
            await client.PostAsJsonAsync(
                "api/courses",
                new CreateCourseRequest("Algorithms", null, "#2563eb", semester.Id)
            )
        ).EnsureSuccessStatusCode();

        var overview = await client.GetFromJsonAsync<CourseProgressOverviewDto>(
            "api/analytics/course-progress"
        );

        Assert.True(overview!.HasActiveSemester);
        Assert.Equal("Winter 2026/27", overview.SemesterName);
        var course = Assert.Single(overview.Courses);
        Assert.Equal("Algorithms", course.Name);
        Assert.Equal("#2563eb", course.Color);
        Assert.Equal(0, course.Flashcards.Total);
        Assert.Null(course.Flashcards.LearnedPercent);
        Assert.Null(course.LatestExamPercent);
    }
}
