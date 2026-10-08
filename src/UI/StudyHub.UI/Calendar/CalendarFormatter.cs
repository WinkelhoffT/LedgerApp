using System.Globalization;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.StudySessions;

namespace StudyHub.UI.Calendar;

/// <summary>Formats calendar periods, days and session times in English with 24-hour times.</summary>
public static class CalendarFormatter
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <summary><c>October 2026</c>.</summary>
    public static string FormatMonth(int year, int month) =>
        new DateOnly(year, month, 1).ToString("MMMM yyyy", Culture);

    /// <summary><c>Week 41 · 5 – 11 Oct 2026</c>, <c>Week 44 · 26 Oct – 1 Nov 2026</c> or <c>Week 53 · 28 Dec 2026 – 3 Jan 2027</c>.</summary>
    public static string FormatWeek(int isoWeek, DateOnly start, DateOnly end)
    {
        var startFormat = start.Year != end.Year ? "d MMM yyyy" : start.Month != end.Month ? "d MMM" : "%d";
        return $"Week {isoWeek} · {start.ToString(startFormat, Culture)} – {end.ToString("d MMM yyyy", Culture)}";
    }

    /// <summary><c>Thursday</c>.</summary>
    public static string FormatWeekday(DateOnly date) => date.ToString("dddd", Culture);

    /// <summary><c>8 October 2026</c>.</summary>
    public static string FormatDate(DateOnly date) => date.ToString("d MMMM yyyy", Culture);

    /// <summary><c>Mon</c>.</summary>
    public static string FormatShortWeekday(DateOnly date) => date.ToString("ddd", Culture);

    /// <summary><c>07:00</c>.</summary>
    public static string FormatHour(int hour) => $"{hour:00}:00";

    /// <summary><c>09:00</c>.</summary>
    public static string FormatTime(TimeOnly time) => time.ToString("HH:mm", Culture);

    /// <summary><c>09:00 – 10:30</c>.</summary>
    public static string FormatTimeRange(StudySessionDto session) =>
        $"{FormatTime(session.StartTime)} – {FormatTime(session.EndTime)}";

    /// <summary><c>90 min</c>.</summary>
    public static string FormatDuration(int minutes) => $"{minutes} min";

    /// <summary>Screen-reader label of a chip or block: <c>Graph review, 09:00 – 10:30, Algorithms</c>.</summary>
    public static string FormatSessionLabel(StudySessionDto session) =>
        string.Join(", ", new[] { session.Title, FormatTimeRange(session), session.OwnerName }.Where(part => !string.IsNullOrEmpty(part)));

    /// <summary>The CSS color of a session: its course color, otherwise the accent color.</summary>
    public static string GetColor(StudySessionDto session) => session.Color ?? "var(--accent)";

    /// <summary>The CSS color of an exam or deadline: its course color, otherwise the accent color.</summary>
    public static string GetColor(CalendarEventDto calendarEvent) => calendarEvent.Color ?? "var(--accent)";

    /// <summary><c>Exam</c> or <c>Deadline</c>.</summary>
    public static string FormatKind(CalendarEventKind kind) => kind switch
    {
        CalendarEventKind.Exam => "Exam",
        CalendarEventKind.Deadline => "Deadline",
        _ => kind.ToString(),
    };

    /// <summary><c>10:00 – 12:00 · 120 min</c> for a timed exam, <c>Due 23:59</c> for a deadline with a due time, otherwise <c>All day</c>.</summary>
    public static string FormatEventTime(CalendarEventDto calendarEvent) => calendarEvent switch
    {
        { StartTime: { } start, EndTime: { } end, DurationMinutes: { } duration } =>
            $"{FormatTime(start)} – {FormatTime(end)} · {FormatDuration(duration)}",
        { Kind: CalendarEventKind.Deadline, StartTime: { } due } => $"Due {FormatTime(due)}",
        _ => "All day",
    };

    /// <summary>The time in front of a chip: the start or due time, nothing for all-day events.</summary>
    public static string? FormatEventChipTime(CalendarEventDto calendarEvent) =>
        calendarEvent.StartTime is { } start ? FormatTime(start) : null;

    /// <summary>Screen-reader label: <c>Exam: Algorithms exam, 10:00 – 12:00 · 120 min, Algorithms</c>.</summary>
    public static string FormatEventLabel(CalendarEventDto calendarEvent) =>
        string.Join(", ", new[]
        {
            $"{FormatKind(calendarEvent.Kind)}: {calendarEvent.Title}",
            FormatEventTime(calendarEvent),
            calendarEvent.OwnerName,
        }.Where(part => !string.IsNullOrEmpty(part)));

    /// <summary><c>Today</c>, <c>Tomorrow</c> or <c>in 12 days</c>.</summary>
    public static string FormatCountdown(int daysUntil) => daysUntil switch
    {
        0 => "Today",
        1 => "Tomorrow",
        _ => $"in {daysUntil} days",
    };

    /// <summary><c>Tue 20 Oct</c>.</summary>
    public static string FormatShortDate(DateOnly date) => date.ToString("ddd d MMM", Culture);
}
