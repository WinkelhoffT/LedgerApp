namespace StudyHub.Shared.CalendarEvents;

public enum CalendarEventKind
{
    /// <summary>An exam, all-day or with a start time and a duration.</summary>
    Exam = 1,

    /// <summary>A submission deadline: a date with an optional due time.</summary>
    Deadline = 2,
}
