using Microsoft.EntityFrameworkCore;
using StudyHub.Data;
using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Infrastructure.Semesters;

public sealed class SemesterRepository(ApplicationDbContext dbContext) : ISemesterRepository
{
    public async Task<IReadOnlyList<Semester>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Semesters
            .OrderBy(s => s.StartDate)
            .ToListAsync(cancellationToken);

    public Task<Semester?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Semesters.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();

        return dbContext.Semesters
            .Where(s => excludingId == null || s.Id != excludingId)
            .AnyAsync(s => s.Name.ToLower() == normalizedName, cancellationToken);
    }

    public async Task AddAsync(Semester semester, CancellationToken cancellationToken = default) =>
        await dbContext.Semesters.AddAsync(semester, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
