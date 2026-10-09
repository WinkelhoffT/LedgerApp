using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Analytics;

namespace StudyHub.UI.Components.Shared;

/// <summary>
/// Whole weeks of study days, Monday to Sunday, shaded by the Api's heat level (fixed boundaries, so
/// a color means the same every week). Days still to come stay empty.
/// </summary>
public partial class StudyHeatmap
{
    [Parameter, EditorRequired]
    public IReadOnlyList<StudyHeatmapDayDto> Days { get; set; } = [];

    private static string GetCellClass(StudyHeatmapDayDto day) =>
        day.IsFuture ? "is-future" : $"level-{day.Level}";
}
