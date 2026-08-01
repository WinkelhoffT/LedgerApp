using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Logic.Business.Semesters;

public sealed class SemesterManagement(ISemesterRepository semesterRepository) : ISemesterManagement
{
    public async Task<IReadOnlyList<SemesterDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var semesters = await semesterRepository.GetAllAsync(cancellationToken);
        return semesters.Select(ToDto).ToList();
    }

    public async Task<SemesterDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var semester = await GetExistingSemesterAsync(id, cancellationToken);
        return ToDto(semester);
    }

    public async Task<SemesterDto> CreateAsync(CreateSemesterRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureNameIsUniqueAsync(request.Name, excludingId: null, cancellationToken);

        var semester = Semester.Create(request.Name, request.StartDate, request.EndDate);

        await semesterRepository.AddAsync(semester, cancellationToken);
        await semesterRepository.SaveChangesAsync(cancellationToken);

        return ToDto(semester);
    }

    public async Task<SemesterDto> UpdateAsync(UpdateSemesterRequest request, CancellationToken cancellationToken = default)
    {
        var semester = await GetExistingSemesterAsync(request.Id, cancellationToken);

        await EnsureNameIsUniqueAsync(request.Name, request.Id, cancellationToken);

        semester.Update(request.Name, request.StartDate, request.EndDate);

        await semesterRepository.SaveChangesAsync(cancellationToken);

        return ToDto(semester);
    }

    public async Task<SemesterDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var semester = await GetExistingSemesterAsync(id, cancellationToken);

        semester.Archive();

        await semesterRepository.SaveChangesAsync(cancellationToken);

        return ToDto(semester);
    }

    public async Task<SemesterDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var semester = await GetExistingSemesterAsync(id, cancellationToken);

        semester.Restore();

        await semesterRepository.SaveChangesAsync(cancellationToken);

        return ToDto(semester);
    }

    private async Task<Semester> GetExistingSemesterAsync(Guid id, CancellationToken cancellationToken) =>
        await semesterRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new SemesterNotFoundException(id);

    private async Task EnsureNameIsUniqueAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (await semesterRepository.ExistsByNameAsync(name, excludingId, cancellationToken))
        {
            throw new DuplicateSemesterNameException(name);
        }
    }

    private static SemesterDto ToDto(Semester semester) => new(
        semester.Id,
        semester.Name,
        semester.StartDate,
        semester.EndDate,
        semester.IsArchived,
        semester.CreatedAt,
        semester.UpdatedAt);
}
