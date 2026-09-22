using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract.Courses;
using StudyHub.Shared.Courses;

namespace StudyHub.Api.Courses;

[ApiController]
[Route("api/courses")]
public sealed class CourseController(ICourseOrchestrator courseOrchestrator) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CourseDto>> GetAllAsync(CancellationToken cancellationToken) =>
        courseOrchestrator.GetAllAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<CourseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        courseOrchestrator.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    public Task<CourseDto> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken) =>
        courseOrchestrator.CreateAsync(request, cancellationToken);

    // The route id always wins over whatever Id is present in the request body.
    [HttpPut("{id:guid}")]
    public Task<CourseDto> UpdateAsync(Guid id, UpdateCourseRequest request, CancellationToken cancellationToken) =>
        courseOrchestrator.UpdateAsync(request with { Id = id }, cancellationToken);

    [HttpPost("{id:guid}/archive")]
    public Task<CourseDto> ArchiveAsync(Guid id, CancellationToken cancellationToken) =>
        courseOrchestrator.ArchiveAsync(id, cancellationToken);

    [HttpPost("{id:guid}/restore")]
    public Task<CourseDto> RestoreAsync(Guid id, CancellationToken cancellationToken) =>
        courseOrchestrator.RestoreAsync(id, cancellationToken);
}
