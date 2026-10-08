using System.Globalization;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Domain;

public sealed class CalendarPeriodProvider : ICalendarPeriodProvider
{
    private const int DefaultStartHour = 7;
    private const int DefaultEndHour = 22;
    private const int DaysPerWeek = 7;

    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public CalendarPeriodProvider(CalendarOptions options, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);
    }

    public DateOnly GetToday() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(_timeProvider.GetUtcNow().UtcDateTime, _timeZone));

    public CalendarPeriod GetMonth(int year, int month)
    {
        var first = new DateOnly(year, month, 1);
        var last = first.AddDays(DateTime.DaysInMonth(year, month) - 1);

        return new CalendarPeriod(GetMonday(first), GetMonday(last).AddDays(DaysPerWeek - 1));
    }

    public CalendarPeriod GetWeek(DateOnly date)
    {
        var monday = GetMonday(date);
        return new CalendarPeriod(monday, monday.AddDays(DaysPerWeek - 1));
    }

    public int GetIsoWeek(DateOnly date) =>
        ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue));

    public CalendarHourRange GetWeekHours(IEnumerable<StudySession> sessions)
    {
        var startHour = DefaultStartHour;
        var endHour = DefaultEndHour;

        foreach (var session in sessions)
        {
            var startMinute = (int)session.StartTime.ToTimeSpan().TotalMinutes;
            var endMinute = startMinute + session.DurationMinutes;

            startHour = Math.Min(startHour, startMinute / 60);
            endHour = Math.Max(endHour, (endMinute + 59) / 60);
        }

        return new CalendarHourRange(startHour, Math.Min(endHour, 24));
    }

    // DayOfWeek counts from Sunday (0); ISO weeks start on Monday.
    private static DateOnly GetMonday(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % DaysPerWeek));
}
