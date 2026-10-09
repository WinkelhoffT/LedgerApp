namespace StudyHub.Shared.Analytics;

/// <param name="WeekStart">The Monday of the week.</param>
/// <param name="IsoWeek">ISO 8601 week number (1-53).</param>
public sealed record StudyWeekDto(DateOnly WeekStart, int IsoWeek, int Minutes);
