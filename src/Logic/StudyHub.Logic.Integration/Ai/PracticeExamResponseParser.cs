using System.Text.Json;
using StudyHub.Shared.Ai;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Integration.Ai;

/// <summary>
/// Turns Claude's structured-output response into <see cref="GeneratedPracticeExamTask"/>s. Kept
/// separate from the SDK call so the stop-reason handling and JSON mapping can be unit-tested
/// without the API. Rule checks are left to the Domain validator.
/// </summary>
public static class PracticeExamResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <param name="stopReason">The response's wire-level stop reason (<c>end_turn</c>, <c>max_tokens</c>, <c>refusal</c>, …).</param>
    /// <param name="jsonText">The concatenated text content of the response.</param>
    public static IReadOnlyList<GeneratedPracticeExamTask> Parse(
        string? stopReason,
        string? jsonText
    )
    {
        switch (stopReason)
        {
            case "refusal":
                throw new AiGenerationFailedException(
                    AiGenerationFailureReason.Refused,
                    "Claude declined to write a practice exam from this material."
                );
            case "max_tokens":
                throw new AiGenerationFailedException(
                    AiGenerationFailureReason.Truncated,
                    "The response was cut off before the practice exam was complete. Choose a shorter duration."
                );
        }

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw InvalidResponse("Claude returned an empty response.");
        }

        ResponseBody? body;
        try
        {
            body = JsonSerializer.Deserialize<ResponseBody>(jsonText, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw InvalidResponse("Claude returned malformed JSON.", ex);
        }

        if (body?.Tasks is null)
        {
            throw InvalidResponse("Claude's response did not contain any tasks.");
        }

        return body.Tasks.OfType<ResponseTask>().Select(ToGeneratedTask).ToList();
    }

    private static GeneratedPracticeExamTask ToGeneratedTask(ResponseTask task) =>
        new(
            task.Kind switch
            {
                "single_choice" => PracticeExamTaskKind.SingleChoice,
                "open" => PracticeExamTaskKind.Open,
                _ => null,
            },
            task.Text ?? string.Empty,
            task.Points ?? 0,
            (task.Options ?? [])
                .OfType<ResponseOption>()
                .Select(option => new GeneratedPracticeExamOption(
                    option.Text ?? string.Empty,
                    option.IsCorrect ?? false,
                    option.Rationale ?? string.Empty
                ))
                .ToList(),
            (task.Criteria ?? [])
                .OfType<ResponseCriterion>()
                .Select(criterion => new GeneratedPracticeExamCriterion(
                    criterion.Description ?? string.Empty,
                    criterion.Points ?? 0
                ))
                .ToList(),
            task.Solution ?? string.Empty,
            task.SourceId
        );

    private static AiGenerationFailedException InvalidResponse(
        string message,
        Exception? inner = null
    ) => new(AiGenerationFailureReason.InvalidResponse, message, inner);

    private sealed record ResponseBody(IReadOnlyList<ResponseTask?>? Tasks);

    private sealed record ResponseTask(
        string? Kind,
        string? Text,
        int? Points,
        IReadOnlyList<ResponseOption?>? Options,
        IReadOnlyList<ResponseCriterion?>? Criteria,
        string? Solution,
        int? SourceId
    );

    private sealed record ResponseOption(string? Text, bool? IsCorrect, string? Rationale);

    private sealed record ResponseCriterion(string? Description, int? Points);
}
