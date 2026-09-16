namespace StudyHub.Shared.Notes;

public sealed record CreateNoteRequest(string Title, string Content, string? Tags, Guid? CourseId, Guid? SemesterId);
