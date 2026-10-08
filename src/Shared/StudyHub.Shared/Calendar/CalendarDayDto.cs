namespace StudyHub.Shared.Calendar;

/// <summary>One calendar day with its sessions, ordered by start time.</summary>
public sealed record CalendarDayDto(DateOnly Date, IReadOnlyList<CalendarSessionDto> Sessions);
