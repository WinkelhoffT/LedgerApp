using StudyHub.Logic.Business.Courses;

namespace StudyHub.Api.Courses;

public static class CourseEndpoints
{
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/courses");

        group.MapGet("/", (ICourseManagement courseManagement, CancellationToken cancellationToken)
            => courseManagement.GetAllAsync(cancellationToken));

        group.MapGet("/{id:guid}", (Guid id, ICourseManagement courseManagement, CancellationToken cancellationToken)
            => courseManagement.GetByIdAsync(id, cancellationToken));

        group.MapPost("/", (CreateCourseRequest request, ICourseManagement courseManagement, CancellationToken cancellationToken)
            => courseManagement.CreateAsync(request, cancellationToken));

        // The route id always wins over whatever Id is present in the request body.
        group.MapPut("/{id:guid}", (Guid id, UpdateCourseRequest request, ICourseManagement courseManagement, CancellationToken cancellationToken)
            => courseManagement.UpdateAsync(request with { Id = id }, cancellationToken));

        group.MapPost("/{id:guid}/archive", (Guid id, ICourseManagement courseManagement, CancellationToken cancellationToken)
            => courseManagement.ArchiveAsync(id, cancellationToken));

        group.MapPost("/{id:guid}/restore", (Guid id, ICourseManagement courseManagement, CancellationToken cancellationToken)
            => courseManagement.RestoreAsync(id, cancellationToken));

        return endpoints;
    }
}
