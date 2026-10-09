using StudyHub.Shared.StudySessions;

namespace StudyHub.Shared.Calendar;

/// <summary>A session placed in a calendar day.</summary>
/// <param name="Lane">Zero-based column of the session among the sessions it overlaps with.</param>
/// <param name="LaneCount">Number of columns its group of overlapping sessions needs; 1 without overlaps.</param>
public sealed record CalendarSessionDto(StudySessionDto Session, int Lane, int LaneCount);
