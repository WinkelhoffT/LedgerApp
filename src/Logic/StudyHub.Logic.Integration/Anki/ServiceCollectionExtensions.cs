using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StudyHub.Shared.Configuration;

namespace StudyHub.Logic.Integration.Anki;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the AnkiConnect accessor and validates its options at startup. Called from the
    /// StudyHub.Api composition root only; the UI never talks to Anki directly.
    /// </summary>
    public static IServiceCollection AddStudyHubAnkiConnect(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AnkiConnectOptions>()
            .Bind(configuration.GetSection(AnkiConnectOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IAnkiConnectAccessor, AnkiConnectAccessor>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AnkiConnectOptions>>().Value;
            client.BaseAddress = options.BaseAddress;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }
}
