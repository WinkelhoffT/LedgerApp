using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Documents;

namespace StudyHub.Api.Documents;

[ApiController]
[Route("api/documents")]
public sealed class DocumentController(IDocumentManagement documentManagement) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken cancellationToken) =>
        documentManagement.GetAllAsync(cancellationToken);

    [HttpGet("by-course/{courseId:guid}")]
    public Task<IReadOnlyList<DocumentDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken) =>
        documentManagement.GetByCourseIdAsync(courseId, cancellationToken);

    [HttpGet("by-semester/{semesterId:guid}")]
    public Task<IReadOnlyList<DocumentDto>> GetBySemesterIdAsync(Guid semesterId, CancellationToken cancellationToken) =>
        documentManagement.GetBySemesterIdAsync(semesterId, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<DocumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        documentManagement.GetByIdAsync(id, cancellationToken);

    [HttpGet("{id:guid}/download")]
    public async Task<FileContentResult> DownloadAsync(Guid id, CancellationToken cancellationToken)
    {
        var content = await documentManagement.DownloadAsync(id, cancellationToken);
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
        return await documentManagement.UploadAsync(request, cancellationToken);
    }

    [HttpPost("{id:guid}/archive")]
    public Task<DocumentDto> ArchiveAsync(Guid id, CancellationToken cancellationToken) =>
        documentManagement.ArchiveAsync(id, cancellationToken);

    [HttpPost("{id:guid}/restore")]
    public Task<DocumentDto> RestoreAsync(Guid id, CancellationToken cancellationToken) =>
        documentManagement.RestoreAsync(id, cancellationToken);
}
