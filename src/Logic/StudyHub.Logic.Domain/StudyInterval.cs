namespace StudyHub.Logic.Domain;

/// <summary>A span of study time in UTC, before overlaps are removed.</summary>
/// <param name="Priority">Breaks ties between intervals with the same start; lower goes first.</param>
internal sealed record StudyInterval(DateTime Start, DateTime End, int Priority, Guid? CourseId);
