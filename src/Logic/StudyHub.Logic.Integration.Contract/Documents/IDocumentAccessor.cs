using StudyHub.Shared.Documents;

namespace StudyHub.Logic.Integration.Contract.Documents;

/// <summary>
/// Narrow HTTP access to StudyHub.Api's document endpoints, covering only what the Documents
/// pages and the download proxy actually call - not a full Business-shaped management contract.
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
