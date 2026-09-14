namespace StudyHub.Shared.Documents;

public sealed record UploadDocumentRequest(string FileName, string ContentType, byte[] Content, Guid? CourseId, Guid? SemesterId)
{
    public const int FileNameMaxLength = 260;

    public const int ContentTypeMaxLength = 200;
}
