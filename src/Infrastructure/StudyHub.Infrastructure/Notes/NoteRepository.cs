using Microsoft.EntityFrameworkCore;
using StudyHub.Data;
using StudyHub.Logic.Domain.Notes;

namespace StudyHub.Infrastructure.Notes;

public sealed class NoteRepository(ApplicationDbContext dbContext) : INoteRepository
{
    public async Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Notes
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Note>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        await dbContext.Notes
            .Where(n => n.CourseId == courseId)
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync(cancellationToken);

    public Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Notes.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task AddAsync(Note note, CancellationToken cancellationToken = default) =>
        await dbContext.Notes.AddAsync(note, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
