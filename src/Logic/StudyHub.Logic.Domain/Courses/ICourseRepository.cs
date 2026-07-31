namespace StudyHub.Logic.Domain.Courses;

public interface ICourseRepository
{
    Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, Guid? excludingId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Course course, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
