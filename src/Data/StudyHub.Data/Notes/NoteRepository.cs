using Microsoft.EntityFrameworkCore;
using StudyHub.Logic.Domain.Notes;

namespace StudyHub.Data.Notes;

public sealed class NoteRepository(ApplicationDbContext dbContext) : INoteRepository
{
    public async Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Notes
            .OrderBy(n => n.Title)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Note>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        await dbContext.Notes
            .Where(n => n.CourseId == courseId)
            .OrderBy(n => n.Title)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Note>> GetBySemesterIdAsync(Guid semesterId, CancellationToken cancellationToken = default) =>
        await dbContext.Notes
            .Where(n => n.SemesterId == semesterId)
            .OrderBy(n => n.Title)
            .ToListAsync(cancellationToken);

    public Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Notes.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Note>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await dbContext.Notes
            .Where(n => ids.Contains(n.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByTitleAsync(string title, Guid? excludingId, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim().ToLower();

        return dbContext.Notes
            .Where(n => excludingId == null || n.Id != excludingId)
            .AnyAsync(n => n.Title.ToLower() == normalizedTitle, cancellationToken);
    }

    public async Task AddAsync(Note note, CancellationToken cancellationToken = default) =>
        await dbContext.Notes.AddAsync(note, cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetAttachedDocumentIdsAsync(Guid noteId, CancellationToken cancellationToken = default) =>
        await dbContext.NoteDocuments
            .Where(nd => nd.NoteId == noteId)
            .Select(nd => nd.DocumentId)
            .ToListAsync(cancellationToken);

    public async Task AddAttachmentAsync(Guid noteId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.NoteDocuments
            .AnyAsync(nd => nd.NoteId == noteId && nd.DocumentId == documentId, cancellationToken);

        if (!exists)
        {
            await dbContext.NoteDocuments.AddAsync(NoteDocument.Create(noteId, documentId), cancellationToken);
        }
    }

    public async Task RemoveAttachmentAsync(Guid noteId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var attachment = await dbContext.NoteDocuments
            .FirstOrDefaultAsync(nd => nd.NoteId == noteId && nd.DocumentId == documentId, cancellationToken);

        if (attachment is not null)
        {
            dbContext.NoteDocuments.Remove(attachment);
        }
    }

    public async Task ReplaceLinksAsync(Guid sourceNoteId, IReadOnlyCollection<Guid> targetNoteIds, CancellationToken cancellationToken = default)
    {
        var existingLinks = await dbContext.NoteLinks
            .Where(l => l.SourceNoteId == sourceNoteId)
            .ToListAsync(cancellationToken);

        dbContext.NoteLinks.RemoveRange(existingLinks);

        foreach (var targetNoteId in targetNoteIds)
        {
            await dbContext.NoteLinks.AddAsync(NoteLink.Create(sourceNoteId, targetNoteId), cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Guid>> GetLinkedNoteIdsAsync(Guid noteId, CancellationToken cancellationToken = default) =>
        await dbContext.NoteLinks
            .Where(l => l.SourceNoteId == noteId)
            .Select(l => l.TargetNoteId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetBacklinkNoteIdsAsync(Guid noteId, CancellationToken cancellationToken = default) =>
        await dbContext.NoteLinks
            .Where(l => l.TargetNoteId == noteId)
            .Select(l => l.SourceNoteId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<string, Guid>> GetIdsByTitlesAsync(IReadOnlyCollection<string> titles, CancellationToken cancellationToken = default)
    {
        if (titles.Count == 0)
        {
            return new Dictionary<string, Guid>();
        }

        var normalizedTitles = titles.Select(t => t.Trim().ToLower()).ToList();

        var matches = await dbContext.Notes
            .Where(n => normalizedTitles.Contains(n.Title.ToLower()))
            .Select(n => new { n.Id, n.Title })
            .ToListAsync(cancellationToken);

        return matches.ToDictionary(m => m.Title, m => m.Id, StringComparer.OrdinalIgnoreCase);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
