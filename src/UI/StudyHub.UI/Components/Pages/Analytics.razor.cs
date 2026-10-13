using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Analytics;
using StudyHub.Shared.Analytics;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class Analytics
{
    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    [Inject]
    private IAnalyticsAccessor AnalyticsAccessor { get; set; } = default!;

    private StudyTimeStatisticsDto? StudyTime { get; set; }

    private CourseProgressOverviewDto? CourseProgress { get; set; }

    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        PageHeader.SetHeader("Analytics", "Your productivity at a glance");

        try
        {
            StudyTime = await AnalyticsAccessor.GetStudyTimeAsync();
            CourseProgress = await AnalyticsAccessor.GetCourseProgressAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
    }
}
