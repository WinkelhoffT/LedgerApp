using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StudyHub.Api;
using StudyHub.Data;
using StudyHub.Logic.Integration.Anki;
using StudyHub.Shared.Anki;
using StudyHub.Shared.Dashboard;
using StudyHub.Shared.Semesters;

namespace StudyHub.Tests.Api.Dashboard;

public class DashboardEndpointsTests
{
    private static WebApplicationFactory<Program> CreateFactory(
        IAnkiConnectAccessor? ankiConnectAccessor = null,
        bool ankiEnabled = true
    )
    {
        var databaseName = Guid.NewGuid().ToString();

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // AddStudyHubData resolves/creates the SQLite connection string's directory before
            // the InMemory override below applies; point it at a writable temp path instead of
            // the production default ("/app/data") so that resolution doesn't throw in tests.
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                $"Data Source={Path.Combine(Path.GetTempPath(), $"studyhub-tests-{databaseName}.db")}"
            );
            builder.UseSetting("AnkiConnect:Enabled", ankiEnabled.ToString());

            builder.ConfigureServices(services =>
            {
                if (ankiConnectAccessor is not null)
                {
                    services.RemoveAll<IAnkiConnectAccessor>();
                    services.AddSingleton(ankiConnectAccessor);
                }

                // EF Core 10 also registers the provider through IDbContextOptionsConfiguration, so
                // both registrations must go before switching to the InMemory provider.
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName)
                );
            });
        });
    }

    [Fact]
    public async Task GetSemesterProgress_WithNoSemesters_ReturnsEmptyState()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var progress = await client.GetFromJsonAsync<SemesterProgressDto>(
            "api/dashboard/semester-progress"
        );

        Assert.False(progress!.HasActiveSemester);
    }

    [Fact]
    public async Task GetSemesterProgress_WithSemesterCoveringToday_ReturnsActiveProgress()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await client.PostAsJsonAsync(
            "api/semesters",
            new CreateSemesterRequest("Winter 2026/27", today.AddDays(-10), today.AddDays(10))
        );

        var progress = await client.GetFromJsonAsync<SemesterProgressDto>(
            "api/dashboard/semester-progress"
        );

        Assert.True(progress!.HasActiveSemester);
        Assert.Equal("Winter 2026/27", progress.SemesterName);
        Assert.Equal(11, progress.ElapsedDays);
        Assert.Equal(10, progress.RemainingDays);
    }

    [Fact]
    public async Task GetAnkiStatus_WhenAnkiHasDueCards_ReturnsConnectedStatus()
    {
        var accessor = new Mock<IAnkiConnectAccessor>();
        accessor
            .Setup(a => a.GetDeckCountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new AnkiDeckCountsDto("Informatik", NewCount: 10, LearnCount: 2, ReviewCount: 30),
                    new AnkiDeckCountsDto("Informatik::Algorithmen", NewCount: 4, LearnCount: 1, ReviewCount: 10),
                ]
            );
        using var factory = CreateFactory(accessor.Object);
        using var client = factory.CreateClient();

        var status = await client.GetFromJsonAsync<AnkiStudyStatusDto>("api/dashboard/anki-status");

        Assert.Equal(AnkiConnectionStatus.Connected, status!.Status);
        Assert.True(status.HasCardsToStudy);
        Assert.Equal(42, status.TotalCount);
        Assert.Equal(["Informatik"], status.Decks.Select(deck => deck.DeckName));
    }

    [Fact]
    public async Task GetAnkiStatus_WhenAnkiIsUnreachable_ReturnsOkWithUnavailableStatus()
    {
        var accessor = new Mock<IAnkiConnectAccessor>();
        accessor
            .Setup(a => a.GetDeckCountsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AnkiConnectUnavailableException("Connection refused"));
        using var factory = CreateFactory(accessor.Object);
        using var client = factory.CreateClient();

        var status = await client.GetFromJsonAsync<AnkiStudyStatusDto>("api/dashboard/anki-status");

        Assert.Equal(AnkiConnectionStatus.Unavailable, status!.Status);
        Assert.False(status.HasCardsToStudy);
    }

    [Fact]
    public async Task GetAnkiStatus_WhenDisabled_ReturnsDisabledStatus()
    {
        var accessor = new Mock<IAnkiConnectAccessor>(MockBehavior.Strict);
        using var factory = CreateFactory(accessor.Object, ankiEnabled: false);
        using var client = factory.CreateClient();

        var status = await client.GetFromJsonAsync<AnkiStudyStatusDto>("api/dashboard/anki-status");

        Assert.Equal(AnkiConnectionStatus.Disabled, status!.Status);
    }
}
