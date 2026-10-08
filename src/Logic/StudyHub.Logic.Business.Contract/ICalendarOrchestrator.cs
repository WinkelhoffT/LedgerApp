using StudyHub.Shared.Calendar;

namespace StudyHub.Logic.Business.Contract;

public interface ICalendarOrchestrator
{
    Task<CalendarMonthDto> GetMonthAsync(int year, int month, CancellationToken cancellationToken = default);

    /// <summary>The week (Monday to Sunday) that contains <paramref name="date"/>.</summary>
    Task<CalendarWeekDto> GetWeekAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<CalendarDayDto> GetTodayAsync(CancellationToken cancellationToken = default);
}
