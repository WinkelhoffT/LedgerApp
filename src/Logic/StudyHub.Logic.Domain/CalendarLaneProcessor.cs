using StudyHub.Logic.Domain.Contract;

namespace StudyHub.Logic.Domain;

public sealed class CalendarLaneProcessor : ICalendarLaneProcessor
{
    public IReadOnlyList<CalendarLane> Assign(IEnumerable<CalendarTimeSlot> slots)
    {
        var ordered = slots
            .OrderBy(s => s.StartTime)
            .ThenByDescending(s => s.DurationMinutes)
            .ToList();

        var result = new List<CalendarLane>(ordered.Count);
        var group = new List<(Guid Id, int Lane)>();
        var laneEnds = new List<int>();
        var groupEnd = 0;

        foreach (var slot in ordered)
        {
            var start = StartMinute(slot);
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

            var end = start + slot.DurationMinutes;
            laneEnds[lane] = end;
            groupEnd = Math.Max(groupEnd, end);
            group.Add((slot.Id, lane));
        }

        CloseGroup();
        return result;

        void CloseGroup()
        {
            result.AddRange(group.Select(g => new CalendarLane(g.Id, g.Lane, laneEnds.Count)));
            group.Clear();
            laneEnds.Clear();
            groupEnd = 0;
        }
    }

    private static int StartMinute(CalendarTimeSlot slot) =>
        (int)slot.StartTime.ToTimeSpan().TotalMinutes;
}
