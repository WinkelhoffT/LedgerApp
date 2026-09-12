using StudyHub.Logic.Business.Documents;

namespace StudyHub.UI.Documents;

/// <summary>
/// Narrow HTTP access to StudyHub.Api's document endpoints, covering only what the Documents
/// pages and the download proxy actually call - not the full <see cref="IDocumentManagement"/>
/// business contract.
/// </summary>
public interface IDocumentAccessor
{
    Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> GetBySemesterIdAsync(Guid semesterId, CancellationToken cancellationToken = default);

    Task<DocumentContentDto> DownloadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DocumentDto> UploadAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default);

    Task<DocumentDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DocumentDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
