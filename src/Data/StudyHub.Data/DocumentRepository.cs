using Microsoft.EntityFrameworkCore;
using StudyHub.Data.Contract;
using StudyHub.Shared.Documents;

namespace StudyHub.Data;

public sealed class DocumentRepository(ApplicationDbContext dbContext) : IDocumentRepository
{
    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Documents
            .AsNoTracking()
            .OrderBy(d => d.FileName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Document>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        await dbContext.Documents
            .AsNoTracking()
            .Where(d => d.CourseId == courseId)
            .OrderBy(d => d.FileName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Document>> GetBySemesterIdAsync(Guid semesterId, CancellationToken cancellationToken = default) =>
        await dbContext.Documents
            .AsNoTracking()
            .Where(d => d.SemesterId == semesterId)
            .OrderBy(d => d.FileName)
            .ToListAsync(cancellationToken);

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default) =>
        await dbContext.Documents.AddAsync(document, cancellationToken);

    public void Update(Document document) =>
        dbContext.Documents.Update(document);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
