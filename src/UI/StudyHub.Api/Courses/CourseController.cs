using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Courses;
using StudyHub.Shared.Courses;

namespace StudyHub.Api.Courses;

[ApiController]
[Route("api/courses")]
public sealed class CourseController(ICourseManagement courseManagement) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CourseDto>> GetAllAsync(CancellationToken cancellationToken) =>
        courseManagement.GetAllAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<CourseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        courseManagement.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    public Task<CourseDto> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken) =>
        courseManagement.CreateAsync(request, cancellationToken);

    // The route id always wins over whatever Id is present in the request body.
    [HttpPut("{id:guid}")]
    public Task<CourseDto> UpdateAsync(Guid id, UpdateCourseRequest request, CancellationToken cancellationToken) =>
        courseManagement.UpdateAsync(request with { Id = id }, cancellationToken);

    [HttpPost("{id:guid}/archive")]
    public Task<CourseDto> ArchiveAsync(Guid id, CancellationToken cancellationToken) =>
        courseManagement.ArchiveAsync(id, cancellationToken);

    [HttpPost("{id:guid}/restore")]
    public Task<CourseDto> RestoreAsync(Guid id, CancellationToken cancellationToken) =>
        courseManagement.RestoreAsync(id, cancellationToken);
}
