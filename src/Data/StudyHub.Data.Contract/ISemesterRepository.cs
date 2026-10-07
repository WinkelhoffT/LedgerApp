using StudyHub.Shared.Semesters;

namespace StudyHub.Data.Contract;

public interface ISemesterRepository
{
    Task<IReadOnlyList<Semester>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Semester?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(Semester semester, CancellationToken cancellationToken = default);

    void Update(Semester semester);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
