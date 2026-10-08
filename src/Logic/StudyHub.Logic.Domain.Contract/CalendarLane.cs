namespace StudyHub.Logic.Domain.Contract;

/// <param name="Id">The <see cref="CalendarTimeSlot.Id"/> the lane belongs to.</param>
/// <param name="Lane">Zero-based column of the slot among the slots it overlaps with.</param>
/// <param name="LaneCount">Number of columns the slot's group of overlapping slots needs.</param>
public sealed record CalendarLane(Guid Id, int Lane, int LaneCount);
