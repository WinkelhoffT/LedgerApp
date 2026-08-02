namespace StudyHub.Logic.Business.Notes;

public interface INoteManagement
{
    Task<IReadOnlyList<NoteDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NoteDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<NoteDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<NoteDto> CreateAsync(CreateNoteRequest request, CancellationToken cancellationToken = default);

    Task<NoteDto> UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken = default);

    Task<NoteDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
