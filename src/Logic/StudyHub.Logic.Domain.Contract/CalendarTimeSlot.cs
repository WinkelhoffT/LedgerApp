namespace StudyHub.Logic.Domain.Contract;

/// <summary>The time a session or a timed exam takes up on its day.</summary>
public sealed record CalendarTimeSlot(Guid Id, TimeOnly StartTime, int DurationMinutes);
