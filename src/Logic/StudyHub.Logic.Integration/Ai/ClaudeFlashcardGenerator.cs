using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Ai;

/// <summary>
/// Generates flashcards with Claude via the official Anthropic SDK. Uses structured output so the
/// response is guaranteed to match <see cref="CardsSchema"/>, and the server-side refusal fallback.
/// </summary>
public sealed class ClaudeFlashcardGenerator(
    IOptions<AnthropicOptions> options,
    ILogger<ClaudeFlashcardGenerator> logger) : IFlashcardGenerator, IDisposable
{
    private const string ServerSideFallbackBeta = "server-side-fallback-2026-07-01";

    private static readonly Dictionary<string, JsonElement> CardsSchema = new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            cards = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        front = new { type = "string" },
                        back = new { type = "string" },
                        tags = new { type = "array", items = new { type = "string" } },
                    },
                    required = new[] { "front", "back", "tags" },
                    additionalProperties = false,
                },
            },
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "cards" }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
    };

    private readonly Lazy<AnthropicClient> client = new(() => CreateClient(options.Value));

    public async Task<IReadOnlyList<FlashcardDto>> GenerateAsync(
        FlashcardGenerationInput input,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new AiNotConfiguredException();
        }

        var parameters = new MessageCreateParams
        {
            Model = settings.Model,
            MaxTokens = settings.MaxTokens,
            Betas = [ServerSideFallbackBeta],
            Fallbacks = new Default(),
            System = FlashcardPrompt.System,
            OutputConfig = new BetaOutputConfig
            {
                Effort = settings.Effort,
                Format = new BetaJsonOutputFormat { Schema = CardsSchema },
            },
            Messages = [new() { Role = Role.User, Content = FlashcardPrompt.BuildUserMessage(input) }],
        };

        BetaMessage response;
        try
        {
            response = await client.Value.Beta.Messages.Create(parameters, cancellationToken);
        }
        catch (AnthropicRateLimitException ex)
        {
            throw Failed(FlashcardGenerationFailureReason.RateLimited, "The Anthropic API rate limit was reached. Try again in a minute.", ex);
        }
        catch (Anthropic5xxException ex)
        {
            throw Failed(FlashcardGenerationFailureReason.ServiceUnavailable, "The Anthropic API is currently unavailable. Try again later.", ex);
        }
        catch (AnthropicUnauthorizedException ex)
        {
            throw Failed(FlashcardGenerationFailureReason.Unauthorized, "The configured Anthropic API key was rejected.", ex);
        }
        catch (AnthropicApiException ex)
        {
            throw Failed(FlashcardGenerationFailureReason.Unknown, "The Anthropic API rejected the flashcard request.", ex);
        }
        catch (AnthropicIOException ex)
        {
            throw Failed(FlashcardGenerationFailureReason.ServiceUnavailable, "The Anthropic API could not be reached.", ex);
        }

        logger.LogInformation(
            "Generated flashcards with {Model} (prompt {PromptVersion}): stop reason {StopReason}, {InputTokens} input / {OutputTokens} output tokens",
            response.Model, FlashcardPrompt.Version, response.StopReason, response.Usage.InputTokens, response.Usage.OutputTokens);

        var text = string.Concat(response.Content.Select(block => block.Value).OfType<BetaTextBlock>().Select(block => block.Text));
        return FlashcardResponseParser.Parse(response.StopReason?.Raw(), text);
    }

    public void Dispose()
    {
        if (client.IsValueCreated)
        {
            client.Value.Dispose();
        }
    }

    private FlashcardGenerationFailedException Failed(FlashcardGenerationFailureReason reason, string message, Exception inner)
    {
        logger.LogWarning(inner, "Flashcard generation failed ({Reason})", reason);
        return new FlashcardGenerationFailedException(reason, message, inner);
    }

    private static AnthropicClient CreateClient(AnthropicOptions settings) =>
        new()
        {
            ApiKey = settings.ApiKey,
            Timeout = settings.RequestTimeout,
            MaxRetries = 1,
        };
}
