namespace StudyHub.Shared.Notes;

public sealed record CreateNoteRequest(string Title, string Content, string? Tags, Guid? CourseId, Guid? SemesterId)
{
    public const int TitleMaxLength = 200;

    public const int ContentMaxLength = 50_000;

    public const int TagsMaxLength = 500;
}
