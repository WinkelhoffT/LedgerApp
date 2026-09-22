using StudyHub.Shared.Notes;

namespace StudyHub.Logic.Business.Contract;

public interface INoteOrchestrator
{
    Task<IReadOnlyList<NoteDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NoteDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NoteDto>> GetBySemesterIdAsync(Guid semesterId, CancellationToken cancellationToken = default);

    Task<NoteDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<NoteDto> CreateAsync(CreateNoteRequest request, CancellationToken cancellationToken = default);

    Task<NoteDto> UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken = default);

    Task<NoteDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<NoteDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    Task<NoteDto> AttachDocumentAsync(Guid noteId, Guid documentId, CancellationToken cancellationToken = default);

    Task<NoteDto> DetachDocumentAsync(Guid noteId, Guid documentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NoteBacklinkDto>> GetBacklinksAsync(Guid noteId, CancellationToken cancellationToken = default);
}
