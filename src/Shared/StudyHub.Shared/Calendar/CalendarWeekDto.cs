namespace StudyHub.Shared.Calendar;

/// <summary>An ISO 8601 week from Monday (<see cref="Start"/>) to Sunday (<see cref="End"/>).</summary>
/// <param name="IsoWeek">ISO 8601 week number (1-53).</param>
/// <param name="Today">The current date in the configured calendar time zone.</param>
/// <param name="StartHour">First hour row of the time grid.</param>
/// <param name="EndHour">Hour at which the time grid ends (exclusive, at most 24).</param>
public sealed record CalendarWeekDto(
    DateOnly Start,
    DateOnly End,
    int IsoWeek,
    DateOnly Today,
    int StartHour,
    int EndHour,
    IReadOnlyList<CalendarDayDto> Days);
