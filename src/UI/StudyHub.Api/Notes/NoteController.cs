using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract.Notes;
using StudyHub.Shared.Notes;

namespace StudyHub.Api.Notes;

[ApiController]
[Route("api/notes")]
public sealed class NoteController(INoteOrchestrator noteOrchestrator) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<NoteDto>> GetAllAsync(CancellationToken cancellationToken) =>
        noteOrchestrator.GetAllAsync(cancellationToken);

    [HttpGet("by-course/{courseId:guid}")]
    public Task<IReadOnlyList<NoteDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken) =>
        noteOrchestrator.GetByCourseIdAsync(courseId, cancellationToken);

    [HttpGet("by-semester/{semesterId:guid}")]
    public Task<IReadOnlyList<NoteDto>> GetBySemesterIdAsync(Guid semesterId, CancellationToken cancellationToken) =>
        noteOrchestrator.GetBySemesterIdAsync(semesterId, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<NoteDto> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        noteOrchestrator.GetByIdAsync(id, cancellationToken);

    [HttpGet("{id:guid}/backlinks")]
    public Task<IReadOnlyList<NoteBacklinkDto>> GetBacklinksAsync(Guid id, CancellationToken cancellationToken) =>
        noteOrchestrator.GetBacklinksAsync(id, cancellationToken);

    [HttpPost]
    public Task<NoteDto> CreateAsync(CreateNoteRequest request, CancellationToken cancellationToken) =>
        noteOrchestrator.CreateAsync(request, cancellationToken);

    // The route id always wins over whatever Id is present in the request body.
    [HttpPut("{id:guid}")]
    public Task<NoteDto> UpdateAsync(Guid id, UpdateNoteRequest request, CancellationToken cancellationToken) =>
        noteOrchestrator.UpdateAsync(request with { Id = id }, cancellationToken);

    [HttpPost("{id:guid}/archive")]
    public Task<NoteDto> ArchiveAsync(Guid id, CancellationToken cancellationToken) =>
        noteOrchestrator.ArchiveAsync(id, cancellationToken);

    [HttpPost("{id:guid}/restore")]
    public Task<NoteDto> RestoreAsync(Guid id, CancellationToken cancellationToken) =>
        noteOrchestrator.RestoreAsync(id, cancellationToken);

    [HttpPost("{id:guid}/documents/{documentId:guid}")]
    public Task<NoteDto> AttachDocumentAsync(Guid id, Guid documentId, CancellationToken cancellationToken) =>
        noteOrchestrator.AttachDocumentAsync(id, documentId, cancellationToken);

    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    public Task<NoteDto> DetachDocumentAsync(Guid id, Guid documentId, CancellationToken cancellationToken) =>
        noteOrchestrator.DetachDocumentAsync(id, documentId, cancellationToken);
}
