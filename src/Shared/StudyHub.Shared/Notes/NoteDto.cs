namespace StudyHub.Shared.Notes;

public sealed record NoteDto(
    Guid Id,
    string Title,
    string Content,
    IReadOnlyList<string> Tags,
    Guid? CourseId,
    Guid? SemesterId,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<Guid> AttachedDocumentIds,
    IReadOnlyList<Guid> LinkedNoteIds);
