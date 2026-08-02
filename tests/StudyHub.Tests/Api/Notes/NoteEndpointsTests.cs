using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyHub.Data;
using StudyHub.Logic.Business.Courses;
using StudyHub.Logic.Business.Notes;

namespace StudyHub.Tests.Api.Notes;

public class NoteEndpointsTests
{
    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = Guid.NewGuid().ToString();

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // AddStudyHubData resolves/creates the SQLite connection string's directory before
            // the InMemory override below applies; point it at a writable temp path instead of
            // the production default ("/app/data") so that resolution doesn't throw in tests.
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                $"Data Source={Path.Combine(Path.GetTempPath(), $"studyhub-tests-{databaseName}.db")}");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        });
    }

    private static async Task<Guid> CreateCourseAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "api/courses", new CreateCourseRequest("Algorithms", null, "#2563eb", Guid.NewGuid()));
        var course = await response.Content.ReadFromJsonAsync<CourseDto>();
        return course!.Id;
    }

    [Fact]
    public async Task GetAll_WithNoNotes_ReturnsEmptyList()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var notes = await client.GetFromJsonAsync<List<NoteDto>>("api/notes");

        Assert.Empty(notes!);
    }

    [Fact]
    public async Task Create_ThenGetById_RoundTrips()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var courseId = await CreateCourseAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            "api/notes", new CreateNoteRequest("SOLID Principles", "# SOLID", courseId));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<NoteDto>();

        var fetched = await client.GetFromJsonAsync<NoteDto>($"api/notes/{created!.Id}");

        Assert.Equal("SOLID Principles", fetched!.Title);
        Assert.Equal(courseId, fetched.CourseId);
    }

    [Fact]
    public async Task Create_WithUnknownCourse_Returns404WithErrorCode()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "api/notes", new CreateNoteRequest("Title", "content", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(CourseErrorCodes.CourseNotFound, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task Create_WithoutTitle_Returns400WithErrorCode()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var courseId = await CreateCourseAsync(client);

        var response = await client.PostAsJsonAsync(
            "api/notes", new CreateNoteRequest("", "content", courseId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(NoteErrorCodes.NoteValidationFailed, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task GetByCourseId_ReturnsOnlyNotesForThatCourse()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var courseId = await CreateCourseAsync(client);
        var otherCourseId = await CreateCourseAsync(client);
        await client.PostAsJsonAsync("api/notes", new CreateNoteRequest("SOLID Principles", "content", courseId));
        await client.PostAsJsonAsync("api/notes", new CreateNoteRequest("SQL Joins", "content", otherCourseId));

        var notes = await client.GetFromJsonAsync<List<NoteDto>>($"api/notes/course/{courseId}");

        Assert.Equal(["SOLID Principles"], notes!.Select(n => n.Title));
    }

    [Fact]
    public async Task Archive_ThenUpdate_Returns409WithErrorCode()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var courseId = await CreateCourseAsync(client);
        var createResponse = await client.PostAsJsonAsync(
            "api/notes", new CreateNoteRequest("Title", "content", courseId));
        var created = await createResponse.Content.ReadFromJsonAsync<NoteDto>();

        var archiveResponse = await client.PostAsync($"api/notes/{created!.Id}/archive", content: null);
        Assert.True(archiveResponse.IsSuccessStatusCode);

        var updateResponse = await client.PutAsJsonAsync(
            $"api/notes/{created.Id}", new UpdateNoteRequest(created.Id, "New Title", "content", courseId));

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
        Assert.Equal(NoteErrorCodes.NoteArchived, await GetErrorCodeAsync(updateResponse));
    }

    private static async Task<string?> GetErrorCodeAsync(HttpResponseMessage response)
    {
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        if (problemDetails is null
            || !problemDetails.Extensions.TryGetValue("errorCode", out var value)
            || value is not JsonElement element)
        {
            return null;
        }

        return element.GetString();
    }
}
