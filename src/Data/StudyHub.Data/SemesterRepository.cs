using Microsoft.EntityFrameworkCore;
using StudyHub.Data.Contract;
using StudyHub.Shared.Semesters;

namespace StudyHub.Data;

public sealed class SemesterRepository(ApplicationDbContext dbContext) : ISemesterRepository
{
    public async Task<IReadOnlyList<Semester>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Semesters
            .AsNoTracking()
            .OrderBy(s => s.StartDate)
            .ToListAsync(cancellationToken);

    public Task<Semester?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();

        return dbContext.Semesters
            .AsNoTracking()
            .Where(s => excludingId == null || s.Id != excludingId)
            .AnyAsync(s => s.Name.ToLower() == normalizedName, cancellationToken);
    }

    public async Task AddAsync(Semester semester, CancellationToken cancellationToken = default) =>
        await dbContext.Semesters.AddAsync(semester, cancellationToken);

    public void Update(Semester semester) =>
        dbContext.Semesters.Update(semester);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
