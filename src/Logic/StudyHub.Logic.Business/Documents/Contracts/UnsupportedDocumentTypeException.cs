namespace StudyHub.Logic.Business.Documents;

public sealed class UnsupportedDocumentTypeException(string contentType)
    : Exception($"Content type '{contentType}' is not supported. Only PDF and DOCX files can be uploaded.")
{
    public string ContentType { get; } = contentType;
}
