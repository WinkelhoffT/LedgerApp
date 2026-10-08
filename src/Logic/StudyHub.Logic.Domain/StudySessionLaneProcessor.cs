using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Domain;

public sealed class StudySessionLaneProcessor : IStudySessionLaneProcessor
{
    public IReadOnlyList<StudySessionLane> Assign(IEnumerable<StudySession> sessions)
    {
        var ordered = sessions
            .OrderBy(s => s.StartTime)
            .ThenByDescending(s => s.DurationMinutes)
            .ToList();

        var result = new List<StudySessionLane>(ordered.Count);
        var group = new List<(StudySession Session, int Lane)>();
        var laneEnds = new List<int>();
        var groupEnd = 0;

        foreach (var session in ordered)
        {
            var start = StartMinute(session);
            if (group.Count > 0 && start >= groupEnd)
            {
                CloseGroup();
            }

            var lane = laneEnds.FindIndex(end => end <= start);
            if (lane < 0)
            {
                lane = laneEnds.Count;
                laneEnds.Add(0);
            }

            var end = start + session.DurationMinutes;
            laneEnds[lane] = end;
            groupEnd = Math.Max(groupEnd, end);
            group.Add((session, lane));
        }

        CloseGroup();
        return result;

        void CloseGroup()
        {
            result.AddRange(group.Select(g => new StudySessionLane(g.Session, g.Lane, laneEnds.Count)));
            group.Clear();
            laneEnds.Clear();
            groupEnd = 0;
        }
    }

    private static int StartMinute(StudySession session) =>
        (int)session.StartTime.ToTimeSpan().TotalMinutes;
}
