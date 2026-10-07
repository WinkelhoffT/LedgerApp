using System.Text.Json;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Ai;

/// <summary>
/// Turns Claude's structured-output response into <see cref="FlashcardDto"/>s. Kept separate from
/// the SDK call so the stop-reason handling and JSON mapping can be unit-tested without the API.
/// </summary>
public static class FlashcardResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <param name="stopReason">The response's wire-level stop reason (<c>end_turn</c>, <c>max_tokens</c>, <c>refusal</c>, …).</param>
    /// <param name="jsonText">The concatenated text content of the response.</param>
    public static IReadOnlyList<FlashcardDto> Parse(string? stopReason, string? jsonText)
    {
        switch (stopReason)
        {
            case "refusal":
                throw new FlashcardGenerationFailedException(
                    FlashcardGenerationFailureReason.Refused,
                    "Claude declined to generate flashcards for this note.");
            case "max_tokens":
                throw new FlashcardGenerationFailedException(
                    FlashcardGenerationFailureReason.Truncated,
                    "The response was cut off before all flashcards were complete. Try fewer cards.");
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

        if (body?.Cards is null)
        {
            throw InvalidResponse("Claude's response did not contain any cards.");
        }

        return body.Cards
            .Where(card => card is not null)
            .Select(card => new FlashcardDto(card!.Front ?? string.Empty, card.Back ?? string.Empty, card.Tags ?? []))
            .ToList();
    }

    private static FlashcardGenerationFailedException InvalidResponse(string message, Exception? inner = null) =>
        new(FlashcardGenerationFailureReason.InvalidResponse, message, inner);

    private sealed record ResponseBody(IReadOnlyList<ResponseCard?>? Cards);

    private sealed record ResponseCard(string? Front, string? Back, IReadOnlyList<string>? Tags);
}
