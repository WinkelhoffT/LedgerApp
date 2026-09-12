using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Business.Dashboard;
using StudyHub.UI.Dashboard;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class Dashboard
{
    [Inject]
    private IPageHeaderService PageHeader { get; set; } = default!;

    [Inject]
    private IDashboardAccessor DashboardAccessor { get; set; } = default!;

    private SemesterProgressDto? Progress { get; set; }

    protected override async Task OnInitializedAsync()
    {
        PageHeader.SetHeader("Dashboard", "Welcome back, Anna");
        Progress = await DashboardAccessor.GetSemesterProgressAsync();
    }

    private static string FormatPercent(double? percentComplete) =>
        $"{percentComplete:0}%";

    private readonly record struct DashboardStat(string IconPaths, string Value, string Label);

    private static readonly DashboardStat[] Stats =
    [
        new(Icons.Clock, "24.5h", "Study time this week"),
        new(Icons.Flame, "18", "Day learning streak"),
        new(Icons.Check, "32", "Tasks completed"),
    ];

    private static class Icons
    {
        public const string Clock = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3 2\"/>";
        public const string Flame = "<path d=\"M8.5 14.5A2.5 2.5 0 0 0 11 12c0-1.38-.5-2-1-3-1.072-2.143-.224-4.054 2-6 .5 2.5 2 4.9 4 6.5 2 1.6 3 3.5 3 5.5a7 7 0 1 1-14 0c0-1.153.433-2.294 1-3a2.5 2.5 0 0 0 2.5 2.5z\"/>";
        public const string Check = "<path d=\"M20 6L9 17l-5-5\"/>";
    }
}
