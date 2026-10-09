using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyHub.Shared.Configuration;

namespace StudyHub.Logic.Integration.Ai;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Claude-backed AI services. Called from the StudyHub.Api composition root only;
    /// the UI never talks to the Anthropic API. A missing <c>Anthropic:ApiKey</c> does not fail startup -
    /// it surfaces as <c>AiNotConfiguredException</c> when a feature is used.
    /// </summary>
    public static IServiceCollection AddStudyHubAi(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<AnthropicOptions>(
            configuration.GetSection(AnthropicOptions.SectionName)
        );

        services.AddSingleton<IAiModelCatalog, ConfiguredAiModelCatalog>();
        services.AddSingleton<IFlashcardGenerator, ClaudeFlashcardGenerator>();

        return services;
    }
}
