using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Api.PracticeExams;

/// <summary>Writing a practice exam: the sheet while in progress, the review after submission.</summary>
[ApiController]
[Route("api/practice-exam-attempts")]
public sealed class PracticeExamAttemptController(
    IPracticeExamAttemptOrchestrator attemptOrchestrator
) : ControllerBase
{
    [HttpPost("~/api/practice-exams/{examId:guid}/attempts")]
    public Task<PracticeExamSheetDto> StartAsync(
        Guid examId,
        StartPracticeExamAttemptRequest request,
        CancellationToken cancellationToken
    ) => attemptOrchestrator.StartAsync(examId, request, cancellationToken);

    [HttpGet("{attemptId:guid}/sheet")]
    public Task<PracticeExamSheetDto> GetSheetAsync(
        Guid attemptId,
        CancellationToken cancellationToken
    ) => attemptOrchestrator.GetSheetAsync(attemptId, cancellationToken);

    [HttpPut("{attemptId:guid}/answers/{taskId:guid}")]
    public async Task<NoContentResult> SaveAnswerAsync(
        Guid attemptId,
        Guid taskId,
        SavePracticeExamAnswerRequest request,
        CancellationToken cancellationToken
    )
    {
        await attemptOrchestrator.SaveAnswerAsync(attemptId, taskId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{attemptId:guid}/submit")]
    public Task<PracticeExamReviewDto> SubmitAsync(
        Guid attemptId,
        CancellationToken cancellationToken
    ) => attemptOrchestrator.SubmitAsync(attemptId, cancellationToken);

    [HttpGet("{attemptId:guid}/review")]
    public Task<PracticeExamReviewDto> GetReviewAsync(
        Guid attemptId,
        CancellationToken cancellationToken
    ) => attemptOrchestrator.GetReviewAsync(attemptId, cancellationToken);

    [HttpPut("{attemptId:guid}/answers/{taskId:guid}/grading")]
    public Task<PracticeExamReviewDto> GradeAsync(
        Guid attemptId,
        Guid taskId,
        GradePracticeExamAnswerRequest request,
        CancellationToken cancellationToken
    ) => attemptOrchestrator.GradeAsync(attemptId, taskId, request, cancellationToken);
}
