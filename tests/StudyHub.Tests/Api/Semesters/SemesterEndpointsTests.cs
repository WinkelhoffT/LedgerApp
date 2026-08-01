using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyHub.Data;
using StudyHub.Logic.Business.Semesters;

namespace StudyHub.Tests.Api.Semesters;

public class SemesterEndpointsTests
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

    [Fact]
    public async Task GetAll_WithNoSemesters_ReturnsEmptyList()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var semesters = await client.GetFromJsonAsync<List<SemesterDto>>("api/semesters");

        Assert.Empty(semesters!);
    }

    [Fact]
    public async Task Create_ThenGetById_RoundTrips()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "api/semesters", new CreateSemesterRequest("Winter 2025/26", StartDate, EndDate));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<SemesterDto>();

        var fetched = await client.GetFromJsonAsync<SemesterDto>($"api/semesters/{created!.Id}");

        Assert.Equal("Winter 2025/26", fetched!.Name);
    }

    [Fact]
    public async Task Create_WithDuplicateName_Returns409WithErrorCode()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("api/semesters", new CreateSemesterRequest("Winter 2025/26", StartDate, EndDate));
        var response = await client.PostAsJsonAsync("api/semesters", new CreateSemesterRequest("Winter 2025/26", StartDate, EndDate));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(SemesterErrorCodes.DuplicateSemesterName, await GetErrorCodeAsync(response));
    }

    [Fact]
    public async Task GetById_WithUnknownId_Returns404WithErrorCode()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"api/semesters/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(SemesterErrorCodes.SemesterNotFound, await GetErrorCodeAsync(response));
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
