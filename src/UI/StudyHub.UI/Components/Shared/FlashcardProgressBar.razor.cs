using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Analytics;

namespace StudyHub.UI.Components.Shared;

/// <summary>
/// A course's cards as one stacked bar: mature in the course color, young lighter, learning
/// lightest; the empty track is the new cards.
/// </summary>
public partial class FlashcardProgressBar
{
    [Parameter, EditorRequired]
    public FlashcardProgressDto Progress { get; set; } = default!;

    /// <summary>The course color, a CSS color value.</summary>
    [Parameter, EditorRequired]
    public string Color { get; set; } = string.Empty;
}
