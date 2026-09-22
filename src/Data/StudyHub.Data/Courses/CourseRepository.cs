using Microsoft.EntityFrameworkCore;
using StudyHub.Data.Contract.Courses;
using StudyHub.Shared.Domain.Courses;

namespace StudyHub.Data.Courses;

public sealed class CourseRepository(ApplicationDbContext dbContext) : ICourseRepository
{
    public async Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Courses
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Course>> GetBySemesterIdAsync(Guid semesterId, CancellationToken cancellationToken = default) =>
        await dbContext.Courses
            .Where(c => c.SemesterId == semesterId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Courses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = Course.NormalizeNameForComparison(name);

        return dbContext.Courses
            .Where(c => excludingId == null || c.Id != excludingId)
            .AnyAsync(c => c.Name.ToLower() == normalizedName, cancellationToken);
    }

    public async Task AddAsync(Course course, CancellationToken cancellationToken = default) =>
        await dbContext.Courses.AddAsync(course, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
