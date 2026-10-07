using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Shared.Courses;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Tests.Api.StudySessions;

public class StudySessionEndpointsTests
{
    private static CreateStudySessionRequest CreateRequest(
        Guid? courseId = null,
        int durationMinutes = 90
    ) =>
        new(
            "Graph review",
            courseId,
            null,
            new DateOnly(2026, 10, 8),
            new TimeOnly(9, 0),
            durationMinutes,
            "Library"
        );

    private static async Task<StudySessionDto> CreateSessionAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("api/study-sessions", CreateRequest());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudySessionDto>())!;
    }

    private static async Task<string?> GetErrorCodeAsync(HttpResponseMessage response)
    {
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return
            problemDetails!.Extensions.TryGetValue("errorCode", out var value)
            && value is JsonElement element
            ? element.GetString()
            : null;
    }

    [Fact]
    public async Task Create_ReturnsTheSessionWithItsEndTime()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();

        var session = await CreateSessionAsync(client);

        Assert.Equal("Graph review", session.Title);
        Assert.Equal(new TimeOnly(10, 30), session.EndTime);
        Assert.Equal("Library", session.Location);
    }

    [Fact]
    public async Task Create_WithInvalidDuration_Returns400WithErrorCode()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "api/study-sessions",
            CreateRequest(durationMinutes: 4)
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            StudySessionErrorCodes.StudySessionValidationFailed,
            await GetErrorCodeAsync(response)
        );
    }

    [Fact]
    public async Task Create_WithUnknownCourse_Returns404WithCourseErrorCode()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "api/study-sessions",
            CreateRequest(Guid.NewGuid())
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(CourseErrorCodes.CourseNotFound, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task Update_UsesTheRouteId()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var session = await CreateSessionAsync(client);

        var response = await client.PutAsJsonAsync(
            $"api/study-sessions/{session.Id}",
            new UpdateStudySessionRequest(
                Guid.NewGuid(),
                "Exam planning",
                null,
                null,
                session.Date,
                new TimeOnly(14, 0),
                45,
                null
            )
        );

        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<StudySessionDto>())!;
        Assert.Equal(session.Id, updated.Id);
        Assert.Equal("Exam planning", updated.Title);
        Assert.Null(updated.Location);
    }

    [Fact]
    public async Task Update_WithUnknownId_Returns404WithErrorCode()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();

        var response = await client.PutAsJsonAsync(
            $"api/study-sessions/{id}",
            new UpdateStudySessionRequest(
                id,
                "Exam planning",
                null,
                null,
                new DateOnly(2026, 10, 8),
                new TimeOnly(14, 0),
                45,
                null
            )
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            StudySessionErrorCodes.StudySessionNotFound,
            await GetErrorCodeAsync(response)
        );
    }

    [Fact]
    public async Task Delete_Returns204AndThen404()
    {
        using var factory = InMemoryApiFactory.Create();
        using var client = factory.CreateClient();
        var session = await CreateSessionAsync(client);

        var first = await client.DeleteAsync($"api/study-sessions/{session.Id}");
        var second = await client.DeleteAsync($"api/study-sessions/{session.Id}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal(StudySessionErrorCodes.StudySessionNotFound, await GetErrorCodeAsync(second));
    }
}
