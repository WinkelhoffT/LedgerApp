using System.Text.Json;

namespace StudyHub.Logic.Integration.Ai;

/// <summary>One structured-output call to Claude.</summary>
/// <param name="Purpose">What is being generated, used in error messages (e.g. "flashcard").</param>
/// <param name="Stream">Streams the response, for calls that can run longer than a plain HTTP request should.</param>
internal sealed record ClaudeStructuredOutputRequest(
    string Model,
    int MaxTokens,
    string SystemPrompt,
    string UserMessage,
    IReadOnlyDictionary<string, JsonElement> Schema,
    string PromptVersion,
    string Purpose,
    bool Stream
);
