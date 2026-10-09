namespace StudyHub.Logic.Domain.Contract;

/// <summary>Places the overlapping sessions and timed exams of one day side by side for the week view.</summary>
public interface ICalendarLaneProcessor
{
    /// <summary>
    /// Orders the slots of one day by start time (longer first on a tie) and gives each one the
    /// first lane that is free when it starts. Slots that overlap directly or through a chain form
    /// a group and share its lane count; a slot that starts exactly when another ends does not
    /// overlap it.
    /// </summary>
    IReadOnlyList<CalendarLane> Assign(IEnumerable<CalendarTimeSlot> slots);
}
