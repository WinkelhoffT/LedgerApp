namespace StudyHub.UI.Calendar;

/// <summary>An empty hour in the week view that was clicked to add a session there.</summary>
public readonly record struct CalendarSlot(DateOnly Date, TimeOnly StartTime);
