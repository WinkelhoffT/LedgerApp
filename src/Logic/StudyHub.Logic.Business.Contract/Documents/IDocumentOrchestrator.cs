using StudyHub.Shared.Documents;

namespace StudyHub.Logic.Business.Contract.Documents;

public interface IDocumentOrchestrator
{
    Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> GetBySemesterIdAsync(Guid semesterId, CancellationToken cancellationToken = default);

    Task<DocumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DocumentContentDto> DownloadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DocumentDto> UploadAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default);

    Task<DocumentDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DocumentDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
