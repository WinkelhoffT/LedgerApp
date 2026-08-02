namespace StudyHub.Logic.Business.Notes;

public sealed record NoteDto(
    Guid Id,
    string Title,
    string Content,
    Guid CourseId,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt);
