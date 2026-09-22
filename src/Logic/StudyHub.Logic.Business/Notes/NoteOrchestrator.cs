using System.Text.RegularExpressions;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Documents;
using StudyHub.Logic.Domain.Notes;
using StudyHub.Logic.Domain.Semesters;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Documents;
using StudyHub.Shared.Notes;
using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Business.Notes;

public sealed partial class NoteOrchestrator(
    INoteRepository noteRepository,
    IDocumentRepository documentRepository,
    ICourseRepository courseRepository,
    ISemesterRepository semesterRepository
) : INoteOrchestrator
{
    public async Task<IReadOnlyList<NoteDto>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        var notes = await noteRepository.GetAllAsync(cancellationToken);
        return await ToDtosAsync(notes, cancellationToken);
    }

    public async Task<IReadOnlyList<NoteDto>> GetByCourseIdAsync(
        Guid courseId,
        CancellationToken cancellationToken = default
    )
    {
        var notes = await noteRepository.GetByCourseIdAsync(courseId, cancellationToken);
        return await ToDtosAsync(notes, cancellationToken);
    }

    public async Task<IReadOnlyList<NoteDto>> GetBySemesterIdAsync(
        Guid semesterId,
        CancellationToken cancellationToken = default
    )
    {
        var notes = await noteRepository.GetBySemesterIdAsync(semesterId, cancellationToken);
        return await ToDtosAsync(notes, cancellationToken);
    }

    public async Task<NoteDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var note = await GetExistingNoteAsync(id, cancellationToken);
        return await ToDtoAsync(note, cancellationToken);
    }

    public async Task<NoteDto> CreateAsync(
        CreateNoteRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await EnsureTitleIsUniqueAsync(request.Title, excludingId: null, cancellationToken);
        await EnsureParentIsAssignableAsync(
            request.CourseId,
            request.SemesterId,
            cancellationToken
        );

        var note = Note.Create(
            request.Title,
            request.Content,
            request.Tags,
            request.CourseId,
            request.SemesterId
        );

        await noteRepository.AddAsync(note, cancellationToken);
        await noteRepository.SaveChangesAsync(cancellationToken);

        await ResolveLinksAsync(note, cancellationToken);

        return await ToDtoAsync(note, cancellationToken);
    }

    public async Task<NoteDto> UpdateAsync(
        UpdateNoteRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var note = await GetExistingNoteAsync(request.Id, cancellationToken);

        await EnsureTitleIsUniqueAsync(request.Title, request.Id, cancellationToken);
        await EnsureParentIsAssignableAsync(
            request.CourseId,
            request.SemesterId,
            cancellationToken
        );

        note.Update(
            request.Title,
            request.Content,
            request.Tags,
            request.CourseId,
            request.SemesterId
        );

        await noteRepository.SaveChangesAsync(cancellationToken);

        await ResolveLinksAsync(note, cancellationToken);

        return await ToDtoAsync(note, cancellationToken);
    }

    public async Task<NoteDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var note = await GetExistingNoteAsync(id, cancellationToken);

        note.Archive();

        await noteRepository.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(note, cancellationToken);
    }

    public async Task<NoteDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var note = await GetExistingNoteAsync(id, cancellationToken);

        note.Restore();

        await noteRepository.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(note, cancellationToken);
    }

    public async Task<NoteDto> AttachDocumentAsync(
        Guid noteId,
        Guid documentId,
        CancellationToken cancellationToken = default
    )
    {
        var note = await GetExistingNoteAsync(noteId, cancellationToken);

        _ =
            await documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new DocumentNotFoundException(documentId);

        await noteRepository.AddAttachmentAsync(noteId, documentId, cancellationToken);
        await noteRepository.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(note, cancellationToken);
    }

    public async Task<NoteDto> DetachDocumentAsync(
        Guid noteId,
        Guid documentId,
        CancellationToken cancellationToken = default
    )
    {
        var note = await GetExistingNoteAsync(noteId, cancellationToken);

        await noteRepository.RemoveAttachmentAsync(noteId, documentId, cancellationToken);
        await noteRepository.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(note, cancellationToken);
    }

    public async Task<IReadOnlyList<NoteBacklinkDto>> GetBacklinksAsync(
        Guid noteId,
        CancellationToken cancellationToken = default
    )
    {
        await GetExistingNoteAsync(noteId, cancellationToken);

        var backlinkNoteIds = await noteRepository.GetBacklinkNoteIdsAsync(
            noteId,
            cancellationToken
        );
        if (backlinkNoteIds.Count == 0)
        {
            return [];
        }

        var backlinkNotes = await noteRepository.GetByIdsAsync(backlinkNoteIds, cancellationToken);
        return backlinkNotes
            .OrderBy(n => n.Title)
            .Select(n => new NoteBacklinkDto(n.Id, n.Title))
            .ToList();
    }

    private async Task<Note> GetExistingNoteAsync(Guid id, CancellationToken cancellationToken) =>
        await noteRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new NoteNotFoundException(id);

    private async Task EnsureTitleIsUniqueAsync(
        string title,
        Guid? excludingId,
        CancellationToken cancellationToken
    )
    {
        if (await noteRepository.ExistsByTitleAsync(title, excludingId, cancellationToken))
        {
            throw new DuplicateNoteTitleException(title);
        }
    }

    private async Task EnsureParentIsAssignableAsync(
        Guid? courseId,
        Guid? semesterId,
        CancellationToken cancellationToken
    )
    {
        if (courseId is { } id)
        {
            var course =
                await courseRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new CourseNotFoundException(id);

            if (course.IsArchived)
            {
                throw new CourseArchivedException(id);
            }
        }

        if (semesterId is { } semId)
        {
            var semester =
                await semesterRepository.GetByIdAsync(semId, cancellationToken)
                ?? throw new SemesterNotFoundException(semId);

            if (semester.IsArchived)
            {
                throw new SemesterArchivedException(semId);
            }
        }
    }

    private async Task ResolveLinksAsync(Note note, CancellationToken cancellationToken)
    {
        var titles = ExtractWikiLinkTitles(note.Content);
        if (titles.Count == 0)
        {
            await noteRepository.ReplaceLinksAsync(note.Id, [], cancellationToken);
            await noteRepository.SaveChangesAsync(cancellationToken);
            return;
        }

        var idsByTitle = await noteRepository.GetIdsByTitlesAsync(titles, cancellationToken);
        var targetNoteIds = idsByTitle.Values.Distinct().ToList();

        await noteRepository.ReplaceLinksAsync(note.Id, targetNoteIds, cancellationToken);
        await noteRepository.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyCollection<string> ExtractWikiLinkTitles(string content)
    {
        var titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in WikiLinkPattern().Matches(content))
        {
            var title = match.Groups[1].Value.Trim();
            if (title.Length > 0)
            {
                titles.Add(title);
            }
        }

        return titles;
    }

    private async Task<NoteDto> ToDtoAsync(Note note, CancellationToken cancellationToken)
    {
        var attachedDocumentIds = await noteRepository.GetAttachedDocumentIdsAsync(
            note.Id,
            cancellationToken
        );
        var linkedNoteIds = await noteRepository.GetLinkedNoteIdsAsync(note.Id, cancellationToken);
        return ToDto(note, attachedDocumentIds, linkedNoteIds);
    }

    private async Task<IReadOnlyList<NoteDto>> ToDtosAsync(
        IReadOnlyList<Note> notes,
        CancellationToken cancellationToken
    )
    {
        var dtos = new List<NoteDto>(notes.Count);
        foreach (var note in notes)
        {
            dtos.Add(await ToDtoAsync(note, cancellationToken));
        }

        return dtos;
    }

    private static NoteDto ToDto(
        Note note,
        IReadOnlyList<Guid> attachedDocumentIds,
        IReadOnlyList<Guid> linkedNoteIds
    ) =>
        new(
            note.Id,
            note.Title,
            note.Content,
            SplitTags(note.Tags),
            note.CourseId,
            note.SemesterId,
            note.IsArchived,
            note.CreatedAt,
            note.UpdatedAt,
            attachedDocumentIds,
            linkedNoteIds
        );

    private static IReadOnlyList<string> SplitTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );

    [GeneratedRegex(@"\[\[(.+?)\]\]")]
    private static partial Regex WikiLinkPattern();
}
