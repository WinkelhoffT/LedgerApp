namespace StudyHub.Logic.Domain.Contract;

/// <summary>The hour rows the week view's time grid shows, from <see cref="StartHour"/> up to <see cref="EndHour"/> (exclusive).</summary>
public sealed record CalendarHourRange(int StartHour, int EndHour);
