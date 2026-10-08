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

    private static readonly TimeOnly DefaultStartTime = new(9, 0);

    [Inject]
    private ICalendarAccessor CalendarAccessor { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    /// <summary>The selected day (<c>yyyy-MM-dd</c>); the calendar shows the period that contains it.</summary>
    [SupplyParameterFromQuery(Name = "date")]
    public string? Date { get; set; }

    private CalendarMonthDto? Month { get; set; }

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

    private string PeriodTitle => Month is null ? string.Empty : CalendarFormatter.FormatMonth(Month.Year, Month.Month);

    private IReadOnlyList<CalendarDayDto> VisibleDays => Month?.Days ?? [];

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

    private bool IsLoaded(DateOnly date) =>
        Month is not null && Month.Year == date.Year && Month.Month == date.Month;

    private async Task<bool> LoadAsync(DateOnly date)
    {
        var version = ++_loadVersion;
        ErrorMessage = null;

        try
        {
            var month = await CalendarAccessor.GetMonthAsync(date.Year, date.Month);
            if (version != _loadVersion)
            {
                return false;
            }

            Month = month;
            _today = month.Today;
            return true;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
            return false;
        }
    }

    // Today if it lies in the visible month, otherwise the 1st.
    private DateOnly GetDefaultSelection() =>
        Month!.Today.Year == Month.Year && Month.Today.Month == Month.Month
            ? Month.Today
            : new DateOnly(Month.Year, Month.Month, 1);

    private void SelectDay(DateOnly date) => Navigate(date, replace: true);

    private void ShowToday()
    {
        if (_today is { } today)
        {
            Navigate(today);
        }
    }

    private void ShowPrevious() => ShowMonth(-1);

    private void ShowNext() => ShowMonth(1);

    // Lands on today when the target month contains it, otherwise on the 1st.
    private void ShowMonth(int offset)
    {
        if (Month is null)
        {
            return;
        }

        var first = new DateOnly(Month.Year, Month.Month, 1).AddMonths(offset);
        Navigate(_today is { } today && today.Year == first.Year && today.Month == first.Month ? today : first);
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

    private void Navigate(DateOnly date, bool replace = false) =>
        NavigationManager.NavigateTo($"calendar?date={date.ToString(DateFormat, CultureInfo.InvariantCulture)}", replace: replace);
}
