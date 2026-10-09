using StudyHub.Shared.Calendar;

namespace StudyHub.Logic.Integration.Calendar;

/// <summary>Narrow HTTP access to StudyHub.Api's calendar views, covering what the calendar page shows.</summary>
public interface ICalendarAccessor
{
    Task<CalendarMonthDto> GetMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default
    );

    /// <summary>The week (Monday to Sunday) that contains <paramref name="date"/>.</summary>
    Task<CalendarWeekDto> GetWeekAsync(
        DateOnly date,
        CancellationToken cancellationToken = default
    );
}
