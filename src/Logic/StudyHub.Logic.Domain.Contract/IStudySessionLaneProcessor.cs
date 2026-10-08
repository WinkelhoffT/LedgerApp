using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>Places the overlapping sessions of one day side by side for the week view.</summary>
public interface IStudySessionLaneProcessor
{
    /// <summary>
    /// Orders the sessions of one day by start time (longer first on a tie) and gives each one the
    /// first lane that is free when it starts. Sessions that overlap directly or through a chain
    /// form a group and share its lane count; a session that starts exactly when another ends does
    /// not overlap it.
    /// </summary>
    IReadOnlyList<StudySessionLane> Assign(IEnumerable<StudySession> sessions);
}
