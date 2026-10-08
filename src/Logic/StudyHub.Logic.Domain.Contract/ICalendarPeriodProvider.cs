using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// The periods the calendar views show. Weeks follow ISO 8601: they start on Monday, and week 1 is
/// the week with the year's first Thursday. "Today" is read in the configured time zone
/// (<c>CalendarOptions</c>).
/// </summary>
public interface ICalendarPeriodProvider
{
    DateOnly GetToday();

    /// <summary>Whole weeks from the Monday on or before the 1st to the Sunday on or after the last day of the month.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The month's grid does not fit into the supported date range.</exception>
    CalendarPeriod GetMonth(int year, int month);

    /// <summary>Monday to Sunday of the week that contains <paramref name="date"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The week does not fit into the supported date range.</exception>
    CalendarPeriod GetWeek(DateOnly date);

    int GetIsoWeek(DateOnly date);

    /// <summary>
    /// 07:00 to 22:00, widened to the full hour before the earliest start and after the latest end
    /// of <paramref name="sessions"/>.
    /// </summary>
    CalendarHourRange GetWeekHours(IEnumerable<StudySession> sessions);
}
