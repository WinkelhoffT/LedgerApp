using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyHub.Data;
using StudyHub.Logic.Business.Dashboard;
using StudyHub.Logic.Business.Semesters;

namespace StudyHub.Tests.Api.Dashboard;

public class DashboardEndpointsTests
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
    public async Task GetSemesterProgress_WithNoSemesters_ReturnsEmptyState()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var progress = await client.GetFromJsonAsync<SemesterProgressDto>("api/dashboard/semester-progress");

        Assert.False(progress!.HasActiveSemester);
    }

    [Fact]
    public async Task GetSemesterProgress_WithSemesterCoveringToday_ReturnsActiveProgress()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await client.PostAsJsonAsync(
            "api/semesters", new CreateSemesterRequest("Winter 2026/27", today.AddDays(-10), today.AddDays(10)));

        var progress = await client.GetFromJsonAsync<SemesterProgressDto>("api/dashboard/semester-progress");

        Assert.True(progress!.HasActiveSemester);
        Assert.Equal("Winter 2026/27", progress.SemesterName);
        Assert.Equal(11, progress.ElapsedDays);
        Assert.Equal(10, progress.RemainingDays);
    }
}
