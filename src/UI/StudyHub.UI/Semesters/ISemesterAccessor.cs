using StudyHub.Logic.Business.Semesters;

namespace StudyHub.UI.Semesters;

/// <summary>
/// Narrow HTTP access to StudyHub.Api's semester endpoints, covering only what the Semesters
/// pages actually call - not the full <see cref="ISemesterManagement"/> business contract.
/// </summary>
public interface ISemesterAccessor
{
    Task<IReadOnlyList<SemesterDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SemesterDto> CreateAsync(CreateSemesterRequest request, CancellationToken cancellationToken = default);

    Task<SemesterDto> UpdateAsync(UpdateSemesterRequest request, CancellationToken cancellationToken = default);

    Task<SemesterDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SemesterDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
