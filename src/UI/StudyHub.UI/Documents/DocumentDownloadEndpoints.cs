using StudyHub.Logic.Business.Documents;

namespace StudyHub.UI.Documents;

/// <summary>
/// The API container is not reachable from the browser (it only exposes port 8080 on the Docker
/// network, see docker-compose.yml), so downloads are proxied through the UI's own public web
/// server instead of linking directly to StudyHub.Api.
/// </summary>
public static class DocumentDownloadEndpoints
{
    public static IEndpointRouteBuilder MapDocumentDownloadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/documents/{id:guid}/download", async (Guid id, IDocumentManagement documentManagement, CancellationToken cancellationToken) =>
        {
            var content = await documentManagement.DownloadAsync(id, cancellationToken);
            return Results.File(content.Content, content.ContentType, content.FileName);
        });

        return endpoints;
    }
}
