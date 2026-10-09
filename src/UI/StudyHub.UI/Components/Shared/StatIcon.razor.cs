using Microsoft.AspNetCore.Components;
using StudyHub.UI.Analytics;

namespace StudyHub.UI.Components.Shared;

/// <summary>A clock for study time, a flame for the streak, cards for flashcards, a checked calendar for sessions.</summary>
public partial class StatIcon
{
    [Parameter, EditorRequired]
    public StatIconKind Kind { get; set; }
}
