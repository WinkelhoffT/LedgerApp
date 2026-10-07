using System.Text.Json.Serialization;

namespace StudyHub.Logic.Integration.Anki;

internal sealed record AnkiDeckStatsParams([property: JsonPropertyName("decks")] IReadOnlyList<string> Decks);
