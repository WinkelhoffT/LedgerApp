using System.Text.Json.Serialization;

namespace StudyHub.Logic.Integration.Anki;

/// <summary>One entry of AnkiConnect's <c>getDeckStats</c> result (keyed by deck id).</summary>
internal sealed record AnkiDeckStats(
    [property: JsonPropertyName("deck_id")] long DeckId,
    [property: JsonPropertyName("new_count")] int NewCount,
    [property: JsonPropertyName("learn_count")] int LearnCount,
    [property: JsonPropertyName("review_count")] int ReviewCount);
