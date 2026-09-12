namespace StudyHub.Logic.Domain.Documents;

public sealed class Document
{
    public const int FileNameMaxLength = 260;
    public const int ContentTypeMaxLength = 200;

    private Document()
    {
    }

    public Guid Id { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public byte[] Content { get; private set; } = [];

    public Guid? CourseId { get; private set; }

    public Guid? SemesterId { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static Document Create(string fileName, string contentType, byte[] content, Guid? courseId, Guid? semesterId)
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
        };

        document.SetDetails(fileName, contentType, content, courseId, semesterId);

        return document;
    }

    public void Archive()
    {
        if (IsArchived)
        {
            return;
        }

        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsArchived)
        {
            return;
        }

        IsArchived = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetDetails(string fileName, string contentType, byte[] content, Guid? courseId, Guid? semesterId)
    {
        var trimmedFileName = fileName?.Trim() ?? string.Empty;
        if (trimmedFileName.Length == 0)
        {
            throw new DocumentValidationException("Document file name is required.");
        }

        if (trimmedFileName.Length > FileNameMaxLength)
        {
            throw new DocumentValidationException($"Document file name must not exceed {FileNameMaxLength} characters.");
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
            throw new DocumentValidationException("A document must be assigned to exactly one of a course or a semester.");
        }

        FileName = trimmedFileName;
        ContentType = trimmedContentType;
        Content = content;
        SizeBytes = content.Length;
        CourseId = courseId;
        SemesterId = semesterId;
        UpdatedAt = DateTime.UtcNow;
    }
}
