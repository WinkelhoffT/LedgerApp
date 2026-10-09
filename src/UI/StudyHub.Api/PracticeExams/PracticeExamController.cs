using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract;
using StudyHub.Shared.Ai;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Api.PracticeExams;

[ApiController]
[Route("api/practice-exams")]
public sealed class PracticeExamController(
    IPracticeExamOrchestrator practiceExamOrchestrator,
    IPracticeExamGenerationOrchestrator generationOrchestrator
) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PracticeExamDto>> GetAllAsync(
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken
    ) => practiceExamOrchestrator.GetAllAsync(includeArchived, cancellationToken);

    [HttpGet("models")]
    public IReadOnlyList<AiModelDto> GetModels() => generationOrchestrator.GetAvailableModels();

    // Generating saves the exam right away: a preview would show the solutions before the exam is written.
    [HttpPost("generate")]
    public async Task<CreatedResult> GenerateAsync(
        GeneratePracticeExamRequest request,
        CancellationToken cancellationToken
    )
    {
        var exam = await generationOrchestrator.GenerateAsync(request, cancellationToken);
        return Created($"api/practice-exams/{exam.Id}", exam);
    }

    [HttpGet("{id:guid}")]
    public Task<PracticeExamDto> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        practiceExamOrchestrator.GetByIdAsync(id, cancellationToken);

    [HttpGet("{id:guid}/attempts")]
    public Task<IReadOnlyList<PracticeExamAttemptSummaryDto>> GetAttemptsAsync(
        Guid id,
        CancellationToken cancellationToken
    ) => practiceExamOrchestrator.GetAttemptsAsync(id, cancellationToken);

    [HttpPost("{id:guid}/archive")]
    public Task<PracticeExamDto> ArchiveAsync(Guid id, CancellationToken cancellationToken) =>
        practiceExamOrchestrator.ArchiveAsync(id, cancellationToken);

    [HttpPost("{id:guid}/restore")]
    public Task<PracticeExamDto> RestoreAsync(Guid id, CancellationToken cancellationToken) =>
        practiceExamOrchestrator.RestoreAsync(id, cancellationToken);

    [HttpPost("tasks/{taskId:guid}/exclude")]
    public Task<PracticeExamDto> ExcludeTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken
    ) => practiceExamOrchestrator.ExcludeTaskAsync(taskId, cancellationToken);
}
