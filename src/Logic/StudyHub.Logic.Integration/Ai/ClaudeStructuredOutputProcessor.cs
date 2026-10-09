using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudyHub.Shared.Ai;
using StudyHub.Shared.Configuration;

namespace StudyHub.Logic.Integration.Ai;

/// <summary>
/// Sends structured-output requests to Claude via the official Anthropic SDK, with the server-side
/// refusal fallback. Owns the SDK client and turns SDK exceptions into
/// <see cref="AiGenerationFailedException"/>; the stop reason and the JSON are left to each
/// feature's response parser.
/// </summary>
internal sealed class ClaudeStructuredOutputProcessor(
    IOptions<AnthropicOptions> options,
    ILogger<ClaudeStructuredOutputProcessor> logger
) : IDisposable
{
    private const string ServerSideFallbackBeta = "server-side-fallback-2026-07-01";

    private readonly Lazy<AnthropicClient> client = new(() => CreateClient(options.Value));

    /// <exception cref="AiNotConfiguredException">No API key is configured.</exception>
    /// <exception cref="AiGenerationFailedException">The Anthropic API failed or could not be reached.</exception>
    public async Task<ClaudeStructuredOutput> CreateAsync(
        ClaudeStructuredOutputRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new AiNotConfiguredException();
        }

        var parameters = new MessageCreateParams
        {
            Model = request.Model,
            MaxTokens = request.MaxTokens,
            Betas = [ServerSideFallbackBeta],
            Fallbacks = new Default(),
            System = request.SystemPrompt,
            OutputConfig = new BetaOutputConfig
            {
                Effort = settings.Effort,
                Format = new BetaJsonOutputFormat { Schema = request.Schema },
            },
            Messages = [new() { Role = Role.User, Content = request.UserMessage }],
        };

        BetaMessage response;
        try
        {
            // The SDK's request timeout ends with the response headers, so a streamed response may
            // take longer than Anthropic:RequestTimeout; it is only limited by cancellationToken.
            response = request.Stream
                ? await client
                    .Value.Beta.Messages.CreateStreaming(parameters, cancellationToken)
                    .Aggregate()
                : await client.Value.Beta.Messages.Create(parameters, cancellationToken);
        }
        catch (AnthropicRateLimitException ex)
        {
            throw Failed(
                request,
                AiGenerationFailureReason.RateLimited,
                "The Anthropic API rate limit was reached. Try again in a minute.",
                ex
            );
        }
        catch (Anthropic5xxException ex)
        {
            throw Failed(
                request,
                AiGenerationFailureReason.ServiceUnavailable,
                "The Anthropic API is currently unavailable. Try again later.",
                ex
            );
        }
        catch (AnthropicUnauthorizedException ex)
        {
            throw Failed(
                request,
                AiGenerationFailureReason.Unauthorized,
                "The configured Anthropic API key was rejected.",
                ex
            );
        }
        catch (AnthropicApiException ex)
        {
            throw Failed(
                request,
                AiGenerationFailureReason.Unknown,
                $"The Anthropic API rejected the {request.Purpose} request.",
                ex
            );
        }
        catch (AnthropicSseException ex)
        {
            throw Failed(
                request,
                AiGenerationFailureReason.ServiceUnavailable,
                "The Anthropic API stopped the response before it was complete. Try again later.",
                ex
            );
        }
        catch (AnthropicIOException ex)
        {
            throw Failed(
                request,
                AiGenerationFailureReason.ServiceUnavailable,
                "The Anthropic API could not be reached.",
                ex
            );
        }

        logger.LogInformation(
            "Claude responded with {Model} (prompt {PromptVersion}): stop reason {StopReason}, {InputTokens} input / {OutputTokens} output tokens",
            response.Model,
            request.PromptVersion,
            response.StopReason,
            response.Usage.InputTokens,
            response.Usage.OutputTokens
        );

        var text = string.Concat(
            response
                .Content.Select(block => block.Value)
                .OfType<BetaTextBlock>()
                .Select(block => block.Text)
        );
        return new ClaudeStructuredOutput(response.StopReason?.Raw(), text);
    }

    public void Dispose()
    {
        if (client.IsValueCreated)
        {
            client.Value.Dispose();
        }
    }

    private AiGenerationFailedException Failed(
        ClaudeStructuredOutputRequest request,
        AiGenerationFailureReason reason,
        string message,
        Exception inner
    )
    {
        logger.LogWarning(
            inner,
            "AI generation failed for prompt {PromptVersion} ({Reason})",
            request.PromptVersion,
            reason
        );
        return new AiGenerationFailedException(reason, message, inner);
    }

    private static AnthropicClient CreateClient(AnthropicOptions settings) =>
        new()
        {
            ApiKey = settings.ApiKey,
            Timeout = settings.RequestTimeout,
            MaxRetries = 1,
        };
}
