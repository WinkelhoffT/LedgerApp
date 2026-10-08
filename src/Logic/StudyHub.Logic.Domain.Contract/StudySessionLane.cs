using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Domain.Contract;

/// <param name="Lane">Zero-based column of the session among the sessions it overlaps with.</param>
/// <param name="LaneCount">Number of columns the session's group of overlapping sessions needs.</param>
public sealed record StudySessionLane(StudySession Session, int Lane, int LaneCount);
