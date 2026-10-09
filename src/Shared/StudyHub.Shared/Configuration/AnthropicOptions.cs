namespace StudyHub.Shared.Configuration;

/// <summary>
/// Settings for the Claude (Anthropic API) integration, bound from the <c>Anthropic</c> configuration
/// section of StudyHub.Api. <see cref="ApiKey"/> is never committed with a value: set it locally via
/// <c>dotnet user-secrets set "Anthropic:ApiKey" …</c> and in Docker via <c>Anthropic__ApiKey</c>.
/// </summary>
public sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    public string? ApiKey { get; set; }

    /// <summary>Model used when the caller doesn't pick one; always selectable.</summary>
    public string DefaultModel { get; set; } = "claude-sonnet-5-5";

    /// <summary>
    /// The models users may pick in the UI. Configured in appsettings rather than initialized here,
    /// because the configuration binder appends list entries to an initialized list.
    /// </summary>
    public List<AnthropicModelOption> Models { get; set; } = [];

    /// <summary>One of <c>low</c>, <c>medium</c>, <c>high</c>, <c>xhigh</c>, <c>max</c>.</summary>
    public string Effort { get; set; } = "medium";

    public int MaxTokens { get; set; } = 16_000;

    /// <summary>
    /// Output limit for a practice exam, which with model solutions and rubrics is much longer than a
    /// flashcard set. The call streams, so it is not cut off by <see cref="RequestTimeout"/>.
    /// </summary>
    public int PracticeExamMaxTokens { get; set; } = 32_000;

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(150);
}

public sealed class AnthropicModelOption
{
    public string Id { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}
