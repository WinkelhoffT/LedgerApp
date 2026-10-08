using StudyHub.Shared.CalendarEvents;

namespace StudyHub.Shared.Calendar;

/// <summary>An exam or deadline placed in a calendar day.</summary>
/// <param name="Lane">Zero-based column among the overlapping sessions and timed exams; 0 for events without a duration.</param>
/// <param name="LaneCount">Number of columns its group of overlapping items needs; 1 for events without a duration.</param>
public sealed record CalendarEventEntryDto(CalendarEventDto Event, int Lane, int LaneCount);
