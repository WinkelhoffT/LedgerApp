using System.Globalization;
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
}
