namespace StudyHub.Shared.Documents;

public sealed record Document(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    byte[] Content,
    Guid? CourseId,
    Guid? SemesterId,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
