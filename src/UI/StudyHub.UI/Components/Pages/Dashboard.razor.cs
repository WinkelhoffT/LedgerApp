using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Dashboard;
using StudyHub.Shared.Analytics;
using StudyHub.Shared.Calendar;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Dashboard;
using StudyHub.Shared.Flashcards;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class Dashboard
{
    // Exams and deadlines at most this many days away get a highlighted countdown.
    private const int SoonDays = 3;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    [Inject]
    private IDashboardAccessor DashboardAccessor { get; set; } = default!;

    private SemesterProgressDto? Progress { get; set; }

    private FlashcardsDueDto? FlashcardsDue { get; set; }

    private CalendarDayDto? SessionsToday { get; set; }

    private IReadOnlyList<UpcomingCalendarEventDto>? UpcomingEvents { get; set; }

    private StudyTimeStatisticsDto? StudyTime { get; set; }

    protected override async Task OnInitializedAsync()
    {
        PageHeader.SetHeader("Dashboard", "Welcome back, Anna");
        Progress = await DashboardAccessor.GetSemesterProgressAsync();
        FlashcardsDue = await DashboardAccessor.GetFlashcardsDueAsync();
        SessionsToday = await DashboardAccessor.GetSessionsTodayAsync();
        UpcomingEvents = await DashboardAccessor.GetUpcomingEventsAsync();
        StudyTime = await DashboardAccessor.GetStudyTimeAsync();
    }

    private async Task HandleSessionCompletedAsync()
    {
        SessionsToday = await DashboardAccessor.GetSessionsTodayAsync();
        StudyTime = await DashboardAccessor.GetStudyTimeAsync();
    }

    private static string FormatPercent(double? percentComplete) => $"{percentComplete:0}%";
}
