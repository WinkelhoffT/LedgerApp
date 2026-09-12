using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Integration.Semesters;

/// <summary>
/// Narrow HTTP access to StudyHub.Api's semester endpoints, covering only what the Semesters
/// pages actually call - not a full Business-shaped management contract.
/// </summary>
public interface ISemesterAccessor
{
    Task<IReadOnlyList<SemesterDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SemesterDto> CreateAsync(CreateSemesterRequest request, CancellationToken cancellationToken = default);

    Task<SemesterDto> UpdateAsync(UpdateSemesterRequest request, CancellationToken cancellationToken = default);

    Task<SemesterDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SemesterDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
