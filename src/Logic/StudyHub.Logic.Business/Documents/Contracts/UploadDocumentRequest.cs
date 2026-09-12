namespace StudyHub.Logic.Business.Documents;

public sealed record UploadDocumentRequest(string FileName, string ContentType, byte[] Content, Guid? CourseId, Guid? SemesterId);
