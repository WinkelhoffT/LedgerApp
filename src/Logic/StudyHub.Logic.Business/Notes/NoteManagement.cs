using StudyHub.Logic.Business.Courses;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Notes;

namespace StudyHub.Logic.Business.Notes;

public sealed class NoteManagement(INoteRepository noteRepository, ICourseRepository courseRepository) : INoteManagement
{
    public async Task<IReadOnlyList<NoteDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var notes = await noteRepository.GetAllAsync(cancellationToken);
        return notes.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<NoteDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var notes = await noteRepository.GetByCourseIdAsync(courseId, cancellationToken);
        return notes.Select(ToDto).ToList();
    }

    public async Task<NoteDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var note = await GetExistingNoteAsync(id, cancellationToken);
        return ToDto(note);
    }

    public async Task<NoteDto> CreateAsync(CreateNoteRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCourseExistsAsync(request.CourseId, cancellationToken);

        var note = Note.Create(request.Title, request.Content, request.CourseId);

        await noteRepository.AddAsync(note, cancellationToken);
        await noteRepository.SaveChangesAsync(cancellationToken);

        return ToDto(note);
    }

    public async Task<NoteDto> UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken = default)
    {
        var note = await GetExistingNoteAsync(request.Id, cancellationToken);

        await EnsureCourseExistsAsync(request.CourseId, cancellationToken);

        note.Update(request.Title, request.Content, request.CourseId);

        await noteRepository.SaveChangesAsync(cancellationToken);

        return ToDto(note);
    }

    public async Task<NoteDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var note = await GetExistingNoteAsync(id, cancellationToken);

        note.Archive();

        await noteRepository.SaveChangesAsync(cancellationToken);

        return ToDto(note);
    }

    private async Task<Note> GetExistingNoteAsync(Guid id, CancellationToken cancellationToken) =>
        await noteRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NoteNotFoundException(id);

    private async Task EnsureCourseExistsAsync(Guid courseId, CancellationToken cancellationToken)
    {
        _ = await courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new CourseNotFoundException(courseId);
    }

    private static NoteDto ToDto(Note note) => new(
        note.Id,
        note.Title,
        note.Content,
        note.CourseId,
        note.IsArchived,
        note.CreatedAt,
        note.UpdatedAt);
}
