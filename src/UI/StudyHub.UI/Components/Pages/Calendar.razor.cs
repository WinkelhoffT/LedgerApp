using System.Globalization;
using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Calendar;
using StudyHub.Shared.Calendar;
using StudyHub.Shared.StudySessions;
using StudyHub.UI.Calendar;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class Calendar
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string WeekView = "week";
    private const string MonthView = "month";
    private const int DaysPerWeek = 7;

    private static readonly TimeOnly DefaultStartTime = new(9, 0);

    [Inject]
    private ICalendarAccessor CalendarAccessor { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    /// <summary><c>month</c> (default) or <c>week</c>.</summary>
    [SupplyParameterFromQuery(Name = "view")]
    public string? View { get; set; }

    /// <summary>The selected day (<c>yyyy-MM-dd</c>); the calendar shows the month or week that contains it.</summary>
    [SupplyParameterFromQuery(Name = "date")]
    public string? Date { get; set; }

    private bool IsWeek { get; set; }

    private CalendarMonthDto? Month { get; set; }

    private CalendarWeekDto? Week { get; set; }

    private DateOnly SelectedDate { get; set; }

    private string? ErrorMessage { get; set; }

    private bool IsDialogOpen { get; set; }

    private StudySessionDto? EditingSession { get; set; }

    private DateOnly NewSessionDate { get; set; }

    private TimeOnly NewSessionStartTime { get; set; }

    // The latest "today" the Api reported; it follows the Api's calendar time zone, not this host's clock.
    private DateOnly? _today;

    // Navigating quickly starts overlapping loads; only the latest one may update the page.
    private int _loadVersion;

    /// <summary>Today as reported with the period on screen; <c>null</c> until that period has loaded.</summary>
    private DateOnly? Today => IsWeek ? Week?.Today : Month?.Today;

    private string PeriodTitle => IsWeek
        ? Week is null ? string.Empty : CalendarFormatter.FormatWeek(Week.IsoWeek, Week.Start, Week.End)
        : Month is null ? string.Empty : CalendarFormatter.FormatMonth(Month.Year, Month.Month);

    private IReadOnlyList<CalendarDayDto> VisibleDays => (IsWeek ? Week?.Days : Month?.Days) ?? [];

    private IReadOnlyList<CalendarSessionDto> SelectedDaySessions =>
        VisibleDays.FirstOrDefault(d => d.Date == SelectedDate)?.Sessions ?? [];

    private IReadOnlyList<(string Name, string Color)> LegendCourses =>
        VisibleDays
            .SelectMany(d => d.Sessions)
            .Select(s => s.Session)
            .Where(s => s.CourseId is not null && s.Color is not null)
            .DistinctBy(s => s.CourseId)
            .Select(s => (s.OwnerName ?? string.Empty, s.Color!))
            .OrderBy(c => c.Item1, StringComparer.OrdinalIgnoreCase)
            .ToList();

    protected override async Task OnParametersSetAsync()
    {
        IsWeek = string.Equals(View, WeekView, StringComparison.OrdinalIgnoreCase);
        var requestedDate = DateOnly.TryParseExact(Date, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : (DateOnly?)null;

        var anchor = requestedDate ?? _today ?? DateOnly.FromDateTime(DateTime.Now);
        if (!IsLoaded(anchor) && !await LoadAsync(anchor))
        {
            return;
        }

        // Without a date in the URL, this host's clock only gave a first guess; follow the Api's today.
        if (requestedDate is null && _today is { } today && !IsLoaded(today) && !await LoadAsync(today))
        {
            return;
        }

        SelectedDate = requestedDate ?? GetDefaultSelection();
        PageHeader.SetHeader("Calendar", PeriodTitle);
    }

    private bool IsLoaded(DateOnly date) => IsWeek
        ? Week is not null && Week.Start <= date && date <= Week.End
        : Month is not null && Month.Year == date.Year && Month.Month == date.Month;

    private async Task<bool> LoadAsync(DateOnly date)
    {
        var version = ++_loadVersion;
        var isWeek = IsWeek;
        ErrorMessage = null;

        try
        {
            if (isWeek)
            {
                var week = await CalendarAccessor.GetWeekAsync(date);
                if (version != _loadVersion)
                {
                    return false;
                }

                Week = week;
                _today = week.Today;
            }
            else
            {
                var month = await CalendarAccessor.GetMonthAsync(date.Year, date.Month);
                if (version != _loadVersion)
                {
                    return false;
                }

                Month = month;
                _today = month.Today;
            }

            return true;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
            return false;
        }
    }

    // Today if it lies in the visible period, otherwise the 1st of the month or the Monday of the week.
    private DateOnly GetDefaultSelection()
    {
        var today = Today!.Value;
        if (IsLoaded(today))
        {
            return today;
        }

        return IsWeek ? Week!.Start : new DateOnly(Month!.Year, Month.Month, 1);
    }

    private void SelectDay(DateOnly date) => Navigate(IsWeek, date, replace: true);

    private void ShowView(bool week) => Navigate(week, SelectedDate);

    private void ShowToday()
    {
        if (_today is { } today)
        {
            Navigate(IsWeek, today);
        }
    }

    private void ShowPrevious() => ShowPeriod(-1);

    private void ShowNext() => ShowPeriod(1);

    // Lands on today when the target period contains it, otherwise on its first day.
    private void ShowPeriod(int offset)
    {
        DateOnly first, last;
        if (IsWeek && Week is not null)
        {
            first = Week.Start.AddDays(offset * DaysPerWeek);
            last = first.AddDays(DaysPerWeek - 1);
        }
        else if (!IsWeek && Month is not null)
        {
            first = new DateOnly(Month.Year, Month.Month, 1).AddMonths(offset);
            last = first.AddMonths(1).AddDays(-1);
        }
        else
        {
            return;
        }

        Navigate(IsWeek, _today is { } today && first <= today && today <= last ? today : first);
    }

    private void AddSessionForSelectedDay() => OpenCreate(SelectedDate, DefaultStartTime);

    private void OpenCreate(DateOnly date, TimeOnly startTime)
    {
        EditingSession = null;
        NewSessionDate = date;
        NewSessionStartTime = startTime;
        IsDialogOpen = true;
    }

    private void OpenEdit(StudySessionDto session)
    {
        EditingSession = session;
        IsDialogOpen = true;
    }

    private void CloseDialog()
    {
        IsDialogOpen = false;
        EditingSession = null;
    }

    // Shows the saved session's day, which may lie in another period than the one on screen.
    private async Task HandleSavedAsync(StudySessionDto session)
    {
        if (IsLoaded(session.Date))
        {
            await LoadAsync(SelectedDate);
        }

        if (session.Date != SelectedDate)
        {
            SelectDay(session.Date);
        }
    }

    private async Task HandleDeletedAsync(StudySessionDto _) => await LoadAsync(SelectedDate);

    private void Navigate(bool week, DateOnly date, bool replace = false) =>
        NavigationManager.NavigateTo(
            $"calendar?view={(week ? WeekView : MonthView)}&date={date.ToString(DateFormat, CultureInfo.InvariantCulture)}",
            replace: replace);
}
