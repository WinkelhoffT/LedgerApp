using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract.Documents;
using StudyHub.Shared.Documents;

namespace StudyHub.Api.Documents;

[ApiController]
[Route("api/documents")]
public sealed class DocumentController(IDocumentOrchestrator documentOrchestrator) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken cancellationToken) =>
        documentOrchestrator.GetAllAsync(cancellationToken);

    [HttpGet("by-course/{courseId:guid}")]
    public Task<IReadOnlyList<DocumentDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken) =>
        documentOrchestrator.GetByCourseIdAsync(courseId, cancellationToken);

    [HttpGet("by-semester/{semesterId:guid}")]
    public Task<IReadOnlyList<DocumentDto>> GetBySemesterIdAsync(Guid semesterId, CancellationToken cancellationToken) =>
        documentOrchestrator.GetBySemesterIdAsync(semesterId, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<DocumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        documentOrchestrator.GetByIdAsync(id, cancellationToken);

    [HttpGet("{id:guid}/download")]
    public async Task<FileContentResult> DownloadAsync(Guid id, CancellationToken cancellationToken)
    {
        var content = await documentOrchestrator.DownloadAsync(id, cancellationToken);
        return File(content.Content, content.ContentType, content.FileName);
    }

    [HttpPost]
    public async Task<DocumentDto> UploadAsync(
        IFormFile file,
        [FromForm] Guid? courseId,
        [FromForm] Guid? semesterId,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);

        var request = new UploadDocumentRequest(file.FileName, file.ContentType, buffer.ToArray(), courseId, semesterId);
        return await documentOrchestrator.UploadAsync(request, cancellationToken);
    }

    [HttpPost("{id:guid}/archive")]
    public Task<DocumentDto> ArchiveAsync(Guid id, CancellationToken cancellationToken) =>
        documentOrchestrator.ArchiveAsync(id, cancellationToken);

    [HttpPost("{id:guid}/restore")]
    public Task<DocumentDto> RestoreAsync(Guid id, CancellationToken cancellationToken) =>
        documentOrchestrator.RestoreAsync(id, cancellationToken);
}
