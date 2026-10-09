using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Analytics;

namespace StudyHub.UI.Components.Shared;

/// <summary>Monday to Sunday of this week; the days with study time are filled.</summary>
public partial class StudyStreakStrip
{
    [Parameter, EditorRequired]
    public IReadOnlyList<StudyDayDto> Days { get; set; } = [];
}
