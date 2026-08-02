using StudyHub.Logic.Business.Notes;

namespace StudyHub.Api.Notes;

public static class NoteEndpoints
{
    public static IEndpointRouteBuilder MapNoteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/notes");

        group.MapGet("/", (INoteManagement noteManagement, CancellationToken cancellationToken)
            => noteManagement.GetAllAsync(cancellationToken));

        group.MapGet("/course/{courseId:guid}", (Guid courseId, INoteManagement noteManagement, CancellationToken cancellationToken)
            => noteManagement.GetByCourseIdAsync(courseId, cancellationToken));

        group.MapGet("/{id:guid}", (Guid id, INoteManagement noteManagement, CancellationToken cancellationToken)
            => noteManagement.GetByIdAsync(id, cancellationToken));

        group.MapPost("/", (CreateNoteRequest request, INoteManagement noteManagement, CancellationToken cancellationToken)
            => noteManagement.CreateAsync(request, cancellationToken));

        // The route id always wins over whatever Id is present in the request body.
        group.MapPut("/{id:guid}", (Guid id, UpdateNoteRequest request, INoteManagement noteManagement, CancellationToken cancellationToken)
            => noteManagement.UpdateAsync(request with { Id = id }, cancellationToken));

        group.MapPost("/{id:guid}/archive", (Guid id, INoteManagement noteManagement, CancellationToken cancellationToken)
            => noteManagement.ArchiveAsync(id, cancellationToken));

        return endpoints;
    }
}
