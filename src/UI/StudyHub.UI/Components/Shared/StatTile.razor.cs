using Microsoft.AspNetCore.Components;
using StudyHub.UI.Analytics;

namespace StudyHub.UI.Components.Shared;

/// <summary>A key figure with its icon and an optional comparison, as on the Dashboard and the Analytics page.</summary>
public partial class StatTile
{
    [Parameter, EditorRequired]
    public StatIconKind Icon { get; set; }

    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>A comparison or extra figure shown next to the icon, e.g. <c>+1.5 h vs. last week</c>.</summary>
    [Parameter]
    public string? Delta { get; set; }

    /// <summary><c>up</c>, <c>down</c> or <c>same</c> colors the comparison; without it the comparison is neutral.</summary>
    [Parameter]
    public string? DeltaClass { get; set; }
}
