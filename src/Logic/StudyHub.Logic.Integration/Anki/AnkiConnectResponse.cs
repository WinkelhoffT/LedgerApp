using System.Text.Json.Serialization;

namespace StudyHub.Logic.Integration.Anki;

internal sealed record AnkiConnectResponse<TResult>(
    [property: JsonPropertyName("result")] TResult? Result,
    [property: JsonPropertyName("error")] string? Error);
