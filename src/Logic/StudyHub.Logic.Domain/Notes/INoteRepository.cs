namespace StudyHub.Logic.Domain.Notes;

public interface INoteRepository
{
    Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Note>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Note>> GetBySemesterIdAsync(Guid semesterId, CancellationToken cancellationToken = default);

    Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Note>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<bool> ExistsByTitleAsync(string title, Guid? excludingId, CancellationToken cancellationToken = default);

    Task AddAsync(Note note, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetAttachedDocumentIdsAsync(Guid noteId, CancellationToken cancellationToken = default);

    Task AddAttachmentAsync(Guid noteId, Guid documentId, CancellationToken cancellationToken = default);

    Task RemoveAttachmentAsync(Guid noteId, Guid documentId, CancellationToken cancellationToken = default);

    Task ReplaceLinksAsync(Guid sourceNoteId, IReadOnlyCollection<Guid> targetNoteIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetLinkedNoteIdsAsync(Guid noteId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetBacklinkNoteIdsAsync(Guid noteId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, Guid>> GetIdsByTitlesAsync(IReadOnlyCollection<string> titles, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
