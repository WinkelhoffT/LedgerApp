using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyHub.Api;
using StudyHub.Data;
using StudyHub.Shared.Configuration;

namespace StudyHub.Tests.Api;

/// <summary>StudyHub.Api with its own InMemory database and without an Anthropic API key.</summary>
internal static class InMemoryApiFactory
{
    public static WebApplicationFactory<Program> Create()
    {
        var databaseName = Guid.NewGuid().ToString();

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // AddStudyHubData creates the SQLite file's directory before the InMemory override applies.
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                $"Data Source={Path.Combine(Path.GetTempPath(), $"studyhub-tests-{databaseName}.db")}"
            );

            builder.ConfigureServices(services =>
            {
                // EF Core 10 also registers the provider through IDbContextOptionsConfiguration, so
                // both registrations must go before switching to the InMemory provider.
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName)
                );

                services.PostConfigure<AnthropicOptions>(options => options.ApiKey = null);
            });
        });
    }
}
