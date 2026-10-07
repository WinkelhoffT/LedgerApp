using System.Text.Json.Serialization;

namespace StudyHub.Logic.Integration.Anki;

internal sealed record AnkiConnectRequest(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("params"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Params,
    [property: JsonPropertyName("key"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Key);
