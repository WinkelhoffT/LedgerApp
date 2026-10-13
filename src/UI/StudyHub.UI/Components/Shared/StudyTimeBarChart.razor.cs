using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Analytics;

namespace StudyHub.UI.Components.Shared;

/// <summary>Study time per day of this week as bars, today highlighted and the days still to come dimmed.</summary>
public partial class StudyTimeBarChart
{
    [Parameter, EditorRequired]
    public IReadOnlyList<StudyDayDto> Days { get; set; } = [];

    private int MaxMinutes => Days.Select(d => d.Minutes).DefaultIfEmpty(0).Max();
}
