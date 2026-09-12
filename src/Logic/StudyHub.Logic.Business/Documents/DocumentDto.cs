namespace StudyHub.Logic.Business.Documents;

public sealed record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid? CourseId,
    Guid? SemesterId,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt);
