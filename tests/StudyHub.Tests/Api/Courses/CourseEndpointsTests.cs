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

namespace StudyHub.Tests.Api.Courses;

public class CourseEndpointsTests
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

    [Fact]
    public async Task GetAll_WithNoCourses_ReturnsEmptyList()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var courses = await client.GetFromJsonAsync<List<CourseDto>>("api/courses");

        Assert.Empty(courses!);
    }

    [Fact]
    public async Task Create_ThenGetById_RoundTrips()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "api/courses", new CreateCourseRequest("Algorithms", "Description", "#2563eb"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CourseDto>();

        var fetched = await client.GetFromJsonAsync<CourseDto>($"api/courses/{created!.Id}");

        Assert.Equal("Algorithms", fetched!.Name);
    }

    [Fact]
    public async Task Create_WithDuplicateName_Returns409WithErrorCode()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("api/courses", new CreateCourseRequest("Algorithms", null, "#2563eb"));
        var response = await client.PostAsJsonAsync("api/courses", new CreateCourseRequest("Algorithms", null, "#2563eb"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(CourseErrorCodes.DuplicateCourseName, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task GetById_WithUnknownId_Returns404WithErrorCode()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"api/courses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(CourseErrorCodes.CourseNotFound, await GetErrorCodeAsync(response));
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
