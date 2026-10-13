using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Analytics;
using StudyHub.UI.Analytics;

namespace StudyHub.UI.Components.Shared;

/// <summary>Study time per week as a line with a shaded area, the current week last.</summary>
public partial class StudyTimeLineChart
{
    private const double Width = 460;
    private const double Height = 170;
    private const double VerticalPadding = 10;

    // One chart can appear next to another on a page; each needs its own gradient id.
    private readonly string _gradientId = $"study-area-{Guid.NewGuid():N}";

    [Parameter, EditorRequired]
    public IReadOnlyList<StudyWeekDto> Weeks { get; set; } = [];

    private IReadOnlyList<ChartPoint> Points { get; set; } = [];

    protected override void OnParametersSet() =>
        Points = AnalyticsFormatter.GetLinePoints(
            Weeks.Select(w => w.Minutes).ToList(),
            Width,
            Height,
            VerticalPadding
        );
}
