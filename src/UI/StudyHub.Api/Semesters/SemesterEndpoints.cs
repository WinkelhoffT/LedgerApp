using StudyHub.Logic.Business.Semesters;

namespace StudyHub.Api.Semesters;

public static class SemesterEndpoints
{
    public static IEndpointRouteBuilder MapSemesterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/semesters");

        group.MapGet("/", (ISemesterManagement semesterManagement, CancellationToken cancellationToken)
            => semesterManagement.GetAllAsync(cancellationToken));

        group.MapGet("/{id:guid}", (Guid id, ISemesterManagement semesterManagement, CancellationToken cancellationToken)
            => semesterManagement.GetByIdAsync(id, cancellationToken));

        group.MapPost("/", (CreateSemesterRequest request, ISemesterManagement semesterManagement, CancellationToken cancellationToken)
            => semesterManagement.CreateAsync(request, cancellationToken));

        // The route id always wins over whatever Id is present in the request body.
        group.MapPut("/{id:guid}", (Guid id, UpdateSemesterRequest request, ISemesterManagement semesterManagement, CancellationToken cancellationToken)
            => semesterManagement.UpdateAsync(request with { Id = id }, cancellationToken));

        group.MapPost("/{id:guid}/archive", (Guid id, ISemesterManagement semesterManagement, CancellationToken cancellationToken)
            => semesterManagement.ArchiveAsync(id, cancellationToken));

        group.MapPost("/{id:guid}/restore", (Guid id, ISemesterManagement semesterManagement, CancellationToken cancellationToken)
            => semesterManagement.RestoreAsync(id, cancellationToken));

        return endpoints;
    }
}
