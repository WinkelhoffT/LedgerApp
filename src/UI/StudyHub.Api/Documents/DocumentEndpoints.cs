using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Documents;

namespace StudyHub.Api.Documents;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/documents");

        group.MapGet("/", (IDocumentManagement documentManagement, CancellationToken cancellationToken)
            => documentManagement.GetAllAsync(cancellationToken));

        group.MapGet("/by-course/{courseId:guid}", (Guid courseId, IDocumentManagement documentManagement, CancellationToken cancellationToken)
            => documentManagement.GetByCourseIdAsync(courseId, cancellationToken));

        group.MapGet("/by-semester/{semesterId:guid}", (Guid semesterId, IDocumentManagement documentManagement, CancellationToken cancellationToken)
            => documentManagement.GetBySemesterIdAsync(semesterId, cancellationToken));

        group.MapGet("/{id:guid}", (Guid id, IDocumentManagement documentManagement, CancellationToken cancellationToken)
            => documentManagement.GetByIdAsync(id, cancellationToken));

        group.MapGet("/{id:guid}/download", async (Guid id, IDocumentManagement documentManagement, CancellationToken cancellationToken) =>
        {
            var content = await documentManagement.DownloadAsync(id, cancellationToken);
            return Results.File(content.Content, content.ContentType, content.FileName);
        });

        group.MapPost("/", async (
            IFormFile file,
            [FromForm] Guid? courseId,
            [FromForm] Guid? semesterId,
            IDocumentManagement documentManagement,
            CancellationToken cancellationToken) =>
        {
            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);

            var request = new UploadDocumentRequest(file.FileName, file.ContentType, buffer.ToArray(), courseId, semesterId);
            return await documentManagement.UploadAsync(request, cancellationToken);
        });

        group.MapPost("/{id:guid}/archive", (Guid id, IDocumentManagement documentManagement, CancellationToken cancellationToken)
            => documentManagement.ArchiveAsync(id, cancellationToken));

        group.MapPost("/{id:guid}/restore", (Guid id, IDocumentManagement documentManagement, CancellationToken cancellationToken)
            => documentManagement.RestoreAsync(id, cancellationToken));

        return endpoints;
    }
}
