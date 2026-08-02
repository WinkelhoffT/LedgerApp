namespace StudyHub.Logic.Domain.Notes;

public interface INoteRepository
{
    Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Note>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Note note, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
