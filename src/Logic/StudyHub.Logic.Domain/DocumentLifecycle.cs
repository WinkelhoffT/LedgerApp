using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Documents;

namespace StudyHub.Logic.Domain;

public sealed class DocumentLifecycle : IDocumentLifecycle
{
    public Document Create(
        string fileName,
        string contentType,
        byte[] content,
        Guid? courseId,
        Guid? semesterId
    )
    {
        var trimmedFileName = fileName?.Trim() ?? string.Empty;
        if (trimmedFileName.Length == 0)
        {
            throw new DocumentValidationException("Document file name is required.");
        }

        if (trimmedFileName.Length > UploadDocumentRequest.FileNameMaxLength)
        {
            throw new DocumentValidationException(
                $"Document file name must not exceed {UploadDocumentRequest.FileNameMaxLength} characters."
            );
        }

        var trimmedContentType = contentType?.Trim() ?? string.Empty;
        if (trimmedContentType.Length == 0)
        {
            throw new DocumentValidationException("Document content type is required.");
        }

        if (content is null || content.Length == 0)
        {
            throw new DocumentValidationException("Document content must not be empty.");
        }

        if (courseId is null == semesterId is null)
        {
            throw new DocumentValidationException(
                "A document must be assigned to exactly one of a course or a semester."
            );
        }

        var now = DateTime.UtcNow;

        return new Document(
            Id: Guid.NewGuid(),
            FileName: trimmedFileName,
            ContentType: trimmedContentType,
            SizeBytes: content.Length,
            Content: content,
            CourseId: courseId,
            SemesterId: semesterId,
            IsArchived: false,
            CreatedAt: now,
            UpdatedAt: now
        );
    }

    public Document Archive(Document document) =>
        document.IsArchived
            ? document
            : document with
            {
                IsArchived = true,
                UpdatedAt = DateTime.UtcNow,
            };

    public Document Restore(Document document) =>
        !document.IsArchived
            ? document
            : document with
            {
                IsArchived = false,
                UpdatedAt = DateTime.UtcNow,
            };
}
