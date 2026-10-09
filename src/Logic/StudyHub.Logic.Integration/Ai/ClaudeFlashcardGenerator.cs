using System.Text.Json;
using Microsoft.Extensions.Options;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Ai;

/// <summary>
/// Generates flashcards with Claude. Uses structured output so the response is guaranteed to match
/// <see cref="CardsSchema"/>.
/// </summary>
internal sealed class ClaudeFlashcardGenerator(
    ClaudeStructuredOutputProcessor structuredOutputProcessor,
    IOptions<AnthropicOptions> options
) : IFlashcardGenerator
{
    private static readonly Dictionary<string, JsonElement> CardsSchema = new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(
            new
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
            }
        ),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "cards" }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
    };

    public async Task<IReadOnlyList<FlashcardDto>> GenerateAsync(
        FlashcardGenerationInput input,
        CancellationToken cancellationToken = default
    )
    {
        var output = await structuredOutputProcessor.CreateAsync(
            new ClaudeStructuredOutputRequest(
                input.Model,
                options.Value.MaxTokens,
                FlashcardPrompt.System,
                FlashcardPrompt.BuildUserMessage(input),
                CardsSchema,
                FlashcardPrompt.Version,
                "flashcard",
                Stream: false
            ),
            cancellationToken
        );

        return FlashcardResponseParser.Parse(output.StopReason, output.Text);
    }
}
