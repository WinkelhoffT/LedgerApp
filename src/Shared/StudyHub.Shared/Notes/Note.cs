namespace StudyHub.Shared.Notes;

public sealed record Note(
    Guid Id,
    string Title,
    string Content,
    string? Tags,
    Guid? CourseId,
    Guid? SemesterId,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public const int TitleMaxLength = 200;
    public const int ContentMaxLength = 50_000;
    public const int TagsMaxLength = 500;
}
