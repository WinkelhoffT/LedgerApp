namespace StudyHub.Logic.Business.Notes;

public sealed record CreateNoteRequest(string Title, string? Content, Guid CourseId);
