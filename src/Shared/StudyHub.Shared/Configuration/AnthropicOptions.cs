namespace StudyHub.Shared.Configuration;

/// <summary>
/// Settings for the Claude (Anthropic API) integration, bound from the <c>Anthropic</c> configuration
/// section. <see cref="ApiKey"/> is never committed: it falls back to the <c>ANTHROPIC_API_KEY</c>
/// environment variable / user secret.
/// </summary>
public sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";
    public const string ApiKeyConfigurationKey = "ANTHROPIC_API_KEY";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-sonnet-5-5";

    /// <summary>One of <c>low</c>, <c>medium</c>, <c>high</c>, <c>xhigh</c>, <c>max</c>.</summary>
    public string Effort { get; set; } = "medium";

    public int MaxTokens { get; set; } = 16_000;

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(150);
}
