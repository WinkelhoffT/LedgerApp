using System.Text.Json;
using Microsoft.Extensions.Options;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Integration.Ai;

/// <summary>
/// Writes practice exams with Claude. Uses structured output so the response is guaranteed to
/// match <see cref="ExamSchema"/>, and streams, since a long exam with model solutions can take
/// minutes.
/// </summary>
internal sealed class ClaudePracticeExamGenerator(
    ClaudeStructuredOutputProcessor structuredOutputProcessor,
    IOptions<AnthropicOptions> options
) : IPracticeExamGenerator
{
    // The per-kind rules (four options with one correct, 1-6 criteria) cannot be expressed here;
    // PracticeExamValidator checks them.
    private static readonly Dictionary<string, JsonElement> ExamSchema = new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(
            new
            {
                tasks = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            kind = new
                            {
                                type = "string",
                                @enum = new[] { "single_choice", "open" },
                            },
                            text = new { type = "string" },
                            points = new { type = "integer" },
                            options = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        text = new { type = "string" },
                                        isCorrect = new { type = "boolean" },
                                        rationale = new { type = "string" },
                                    },
                                    required = new[] { "text", "isCorrect", "rationale" },
                                    additionalProperties = false,
                                },
                            },
                            criteria = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        description = new { type = "string" },
                                        points = new { type = "integer" },
                                    },
                                    required = new[] { "description", "points" },
                                    additionalProperties = false,
                                },
                            },
                            solution = new { type = "string" },
                            sourceId = new { type = "integer" },
                        },
                        required = new[]
                        {
                            "kind",
                            "text",
                            "points",
                            "options",
                            "criteria",
                            "solution",
                            "sourceId",
                        },
                        additionalProperties = false,
                    },
                },
            }
        ),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "tasks" }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
    };

    public string PromptVersion => PracticeExamPrompt.Version;

    public async Task<IReadOnlyList<GeneratedPracticeExamTask>> GenerateAsync(
        PracticeExamGenerationInput input,
        CancellationToken cancellationToken = default
    )
    {
        var output = await structuredOutputProcessor.CreateAsync(
            new ClaudeStructuredOutputRequest(
                input.Model,
                options.Value.PracticeExamMaxTokens,
                PracticeExamPrompt.System,
                PracticeExamPrompt.BuildUserMessage(input),
                ExamSchema,
                PracticeExamPrompt.Version,
                "practice exam",
                Stream: true
            ),
            cancellationToken
        );

        return PracticeExamResponseParser.Parse(output.StopReason, output.Text);
    }
}
