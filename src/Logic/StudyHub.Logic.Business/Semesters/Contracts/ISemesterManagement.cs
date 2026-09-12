namespace StudyHub.Logic.Business.Semesters;

public interface ISemesterManagement
{
    Task<IReadOnlyList<SemesterDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SemesterDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SemesterDto> CreateAsync(CreateSemesterRequest request, CancellationToken cancellationToken = default);

    Task<SemesterDto> UpdateAsync(UpdateSemesterRequest request, CancellationToken cancellationToken = default);

    Task<SemesterDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SemesterDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
