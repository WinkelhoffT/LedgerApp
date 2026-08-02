namespace StudyHub.Logic.Business.Notes;

public sealed record UpdateNoteRequest(Guid Id, string Title, string? Content, Guid CourseId);
