namespace StudyHub.Shared.Calendar;

/// <summary>
/// The month grid: whole weeks from the Monday on or before the 1st to the Sunday on or after the
/// last day of the month, so <see cref="Days"/> also contains days of the neighbouring months.
/// </summary>
/// <param name="Today">The current date in the configured calendar time zone.</param>
public sealed record CalendarMonthDto(
    int Year,
    int Month,
    DateOnly Today,
    IReadOnlyList<CalendarDayDto> Days
)
{
    public const int MinYear = 1;

    // The grid of December 9999 would end in the year 10000, which DateOnly cannot represent.
    public const int MaxYear = 9998;
}
