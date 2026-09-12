using StudyHub.Logic.Business.Courses;
using StudyHub.Logic.Business.Semesters;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Documents;
using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Logic.Business.Documents;

public sealed class DocumentManagement(
    IDocumentRepository documentRepository,
    ICourseRepository courseRepository,
    ISemesterRepository semesterRepository) : IDocumentManagement
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };

    public async Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var documents = await documentRepository.GetAllAsync(cancellationToken);
        return documents.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<DocumentDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var documents = await documentRepository.GetByCourseIdAsync(courseId, cancellationToken);
        return documents.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<DocumentDto>> GetBySemesterIdAsync(Guid semesterId, CancellationToken cancellationToken = default)
    {
        var documents = await documentRepository.GetBySemesterIdAsync(semesterId, cancellationToken);
        return documents.Select(ToDto).ToList();
    }

    public async Task<DocumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await GetExistingDocumentAsync(id, cancellationToken);
        return ToDto(document);
    }

    public async Task<DocumentContentDto> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await GetExistingDocumentAsync(id, cancellationToken);
        return new DocumentContentDto(document.FileName, document.ContentType, document.Content);
    }

    public async Task<DocumentDto> UploadAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureContentTypeIsAllowed(request.ContentType);
        EnsureSizeIsWithinLimit(request.Content.LongLength);

        if (request.CourseId is { } courseId)
        {
            await EnsureCourseIsAssignableAsync(courseId, cancellationToken);
        }

        if (request.SemesterId is { } semesterId)
        {
            await EnsureSemesterIsAssignableAsync(semesterId, cancellationToken);
        }

        var document = Document.Create(request.FileName, request.ContentType, request.Content, request.CourseId, request.SemesterId);

        await documentRepository.AddAsync(document, cancellationToken);
        await documentRepository.SaveChangesAsync(cancellationToken);

        return ToDto(document);
    }

    public async Task<DocumentDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await GetExistingDocumentAsync(id, cancellationToken);

        document.Archive();

        await documentRepository.SaveChangesAsync(cancellationToken);

        return ToDto(document);
    }

    public async Task<DocumentDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await GetExistingDocumentAsync(id, cancellationToken);

        document.Restore();

        await documentRepository.SaveChangesAsync(cancellationToken);

        return ToDto(document);
    }

    private async Task<Document> GetExistingDocumentAsync(Guid id, CancellationToken cancellationToken) =>
        await documentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DocumentNotFoundException(id);

    private static void EnsureContentTypeIsAllowed(string contentType)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new UnsupportedDocumentTypeException(contentType);
        }
    }

    private static void EnsureSizeIsWithinLimit(long sizeBytes)
    {
        if (sizeBytes > MaxFileSizeBytes)
        {
            throw new DocumentTooLargeException(sizeBytes, MaxFileSizeBytes);
        }
    }

    private async Task EnsureCourseIsAssignableAsync(Guid courseId, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new CourseNotFoundException(courseId);

        if (course.IsArchived)
        {
            throw new CourseArchivedException(courseId);
        }
    }

    private async Task EnsureSemesterIsAssignableAsync(Guid semesterId, CancellationToken cancellationToken)
    {
        var semester = await semesterRepository.GetByIdAsync(semesterId, cancellationToken)
            ?? throw new SemesterNotFoundException(semesterId);

        if (semester.IsArchived)
        {
            throw new SemesterArchivedException(semesterId);
        }
    }

    private static DocumentDto ToDto(Document document) => new(
        document.Id,
        document.FileName,
        document.ContentType,
        document.SizeBytes,
        document.CourseId,
        document.SemesterId,
        document.IsArchived,
        document.CreatedAt,
        document.UpdatedAt);
}
