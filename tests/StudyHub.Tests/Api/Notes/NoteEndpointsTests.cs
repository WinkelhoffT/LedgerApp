using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyHub.Data;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Notes;
using StudyHub.Shared.Semesters;

namespace StudyHub.Tests.Api.Notes;

public class NoteEndpointsTests
{
    private static readonly DateOnly StartDate = new(2025, 10, 1);
    private static readonly DateOnly EndDate = new(2026, 3, 31);

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

    private static async Task<Guid> CreateCourseAsync(HttpClient client, string name = "Algorithms")
    {
        var semesterResponse = await client.PostAsJsonAsync("api/semesters", new CreateSemesterRequest("Winter 2025/26", StartDate, EndDate));
        var semester = await semesterResponse.Content.ReadFromJsonAsync<SemesterDto>();

        var courseResponse = await client.PostAsJsonAsync("api/courses", new CreateCourseRequest(name, null, "#2563eb", semester!.Id));
        courseResponse.EnsureSuccessStatusCode();
        var course = await courseResponse.Content.ReadFromJsonAsync<CourseDto>();
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
            "api/notes", new CreateNoteRequest("Lecture 1", "# Intro", "graphs", courseId, null));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<NoteDto>();

        var fetched = await client.GetFromJsonAsync<NoteDto>($"api/notes/{created!.Id}");

        Assert.Equal("Lecture 1", fetched!.Title);
        Assert.Equal(courseId, fetched.CourseId);
        Assert.Equal(["graphs"], fetched.Tags);
    }

    [Fact]
    public async Task Create_WithDuplicateTitle_Returns409WithErrorCode()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var courseId = await CreateCourseAsync(client);

        await client.PostAsJsonAsync("api/notes", new CreateNoteRequest("Lecture 1", "Content", null, courseId, null));
        var response = await client.PostAsJsonAsync("api/notes", new CreateNoteRequest("Lecture 1", "Other content", null, courseId, null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(NoteErrorCodes.DuplicateNoteTitle, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task Create_WithUnknownCourse_Returns404WithErrorCode()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "api/notes", new CreateNoteRequest("Lecture 1", "Content", null, Guid.NewGuid(), null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(CourseErrorCodes.CourseNotFound, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task ArchiveThenRestore_RoundTrips()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var courseId = await CreateCourseAsync(client);
        var createResponse = await client.PostAsJsonAsync("api/notes", new CreateNoteRequest("Lecture 1", "Content", null, courseId, null));
        var note = await createResponse.Content.ReadFromJsonAsync<NoteDto>();

        var archiveResponse = await client.PostAsync($"api/notes/{note!.Id}/archive", content: null);
        var archived = await archiveResponse.Content.ReadFromJsonAsync<NoteDto>();
        Assert.True(archived!.IsArchived);

        var restoreResponse = await client.PostAsync($"api/notes/{note.Id}/restore", content: null);
        var restored = await restoreResponse.Content.ReadFromJsonAsync<NoteDto>();
        Assert.False(restored!.IsArchived);
    }

    [Fact]
    public async Task WikiLink_BetweenTwoNotes_CreatesBacklink()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var courseId = await CreateCourseAsync(client);

        var targetResponse = await client.PostAsJsonAsync("api/notes", new CreateNoteRequest("Graph Theory", "Content", null, courseId, null));
        var target = await targetResponse.Content.ReadFromJsonAsync<NoteDto>();

        var sourceResponse = await client.PostAsJsonAsync(
            "api/notes", new CreateNoteRequest("Lecture 1", "See [[Graph Theory]] for background.", null, courseId, null));
        var source = await sourceResponse.Content.ReadFromJsonAsync<NoteDto>();

        Assert.Equal([target!.Id], source!.LinkedNoteIds);

        var backlinks = await client.GetFromJsonAsync<List<NoteBacklinkDto>>($"api/notes/{target.Id}/backlinks");
        Assert.Equal(["Lecture 1"], backlinks!.Select(b => b.Title));
    }

    [Fact]
    public async Task AttachThenDetachDocument_RoundTrips()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var courseId = await CreateCourseAsync(client);
        var noteResponse = await client.PostAsJsonAsync("api/notes", new CreateNoteRequest("Lecture 1", "Content", null, courseId, null));
        var note = await noteResponse.Content.ReadFromJsonAsync<NoteDto>();

        using var uploadContent = new MultipartFormDataContent
        {
            { new ByteArrayContent([1, 2, 3]), "file", "Slides.pdf" },
            { new StringContent(courseId.ToString()), "courseId" },
        };
        var uploadResponse = await client.PostAsync("api/documents", uploadContent);
        uploadResponse.EnsureSuccessStatusCode();
        var document = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
        var documentId = document.GetProperty("id").GetGuid();

        var attachResponse = await client.PostAsync($"api/notes/{note!.Id}/documents/{documentId}", content: null);
        var attached = await attachResponse.Content.ReadFromJsonAsync<NoteDto>();
        Assert.Equal([documentId], attached!.AttachedDocumentIds);

        var detachResponse = await client.DeleteAsync($"api/notes/{note.Id}/documents/{documentId}");
        var detached = await detachResponse.Content.ReadFromJsonAsync<NoteDto>();
        Assert.Empty(detached!.AttachedDocumentIds);
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
