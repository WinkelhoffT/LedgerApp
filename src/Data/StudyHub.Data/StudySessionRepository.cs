using Microsoft.EntityFrameworkCore;
using StudyHub.Data.Contract;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Data;

public sealed class StudySessionRepository(ApplicationDbContext dbContext) : IStudySessionRepository
{
    public Task<StudySession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.StudySessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<StudySession>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await dbContext.StudySessions
            .AsNoTracking()
            .Where(s => s.Date >= from && s.Date <= to)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(StudySession session, CancellationToken cancellationToken = default) =>
        await dbContext.StudySessions.AddAsync(session, cancellationToken);

    public void Update(StudySession session) =>
        dbContext.StudySessions.Update(session);

    public void Remove(StudySession session) =>
        dbContext.StudySessions.Remove(session);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
