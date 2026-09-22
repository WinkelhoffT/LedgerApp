using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract.Semesters;
using StudyHub.Shared.Semesters;

namespace StudyHub.Api.Semesters;

[ApiController]
[Route("api/semesters")]
public sealed class SemesterController(ISemesterOrchestrator semesterOrchestrator) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<SemesterDto>> GetAllAsync(CancellationToken cancellationToken) =>
        semesterOrchestrator.GetAllAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<SemesterDto> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        semesterOrchestrator.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    public Task<SemesterDto> CreateAsync(CreateSemesterRequest request, CancellationToken cancellationToken) =>
        semesterOrchestrator.CreateAsync(request, cancellationToken);

    // The route id always wins over whatever Id is present in the request body.
    [HttpPut("{id:guid}")]
    public Task<SemesterDto> UpdateAsync(Guid id, UpdateSemesterRequest request, CancellationToken cancellationToken) =>
        semesterOrchestrator.UpdateAsync(request with { Id = id }, cancellationToken);

    [HttpPost("{id:guid}/archive")]
    public Task<SemesterDto> ArchiveAsync(Guid id, CancellationToken cancellationToken) =>
        semesterOrchestrator.ArchiveAsync(id, cancellationToken);

    [HttpPost("{id:guid}/restore")]
    public Task<SemesterDto> RestoreAsync(Guid id, CancellationToken cancellationToken) =>
        semesterOrchestrator.RestoreAsync(id, cancellationToken);
}
