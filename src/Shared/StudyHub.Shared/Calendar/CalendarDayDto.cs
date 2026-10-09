namespace StudyHub.Shared.Calendar;

/// <summary>One calendar day with its sessions, ordered by start time, and its exams and deadlines, all-day ones first.</summary>
public sealed record CalendarDayDto(
    DateOnly Date,
    IReadOnlyList<CalendarSessionDto> Sessions,
    IReadOnlyList<CalendarEventEntryDto> Events
);
