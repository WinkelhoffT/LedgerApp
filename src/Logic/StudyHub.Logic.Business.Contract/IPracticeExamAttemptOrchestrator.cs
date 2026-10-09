using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Business.Contract;

public interface IPracticeExamAttemptOrchestrator
{
    /// <summary>Resumes the exam's open attempt, or starts a new one.</summary>
    Task<PracticeExamSheetDto> StartAsync(
        Guid examId,
        StartPracticeExamAttemptRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>The exam sheet of an attempt in progress, without solutions.</summary>
    Task<PracticeExamSheetDto> GetSheetAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default
    );

    Task SaveAnswerAsync(
        Guid attemptId,
        Guid taskId,
        SavePracticeExamAnswerRequest request,
        CancellationToken cancellationToken = default
    );

    Task<PracticeExamReviewDto> SubmitAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default
    );

    /// <summary>A submitted attempt with solutions, rationales and rubrics.</summary>
    Task<PracticeExamReviewDto> GetReviewAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Self-grades an open answer with the rubric criteria it meets.</summary>
    Task<PracticeExamReviewDto> GradeAsync(
        Guid attemptId,
        Guid taskId,
        GradePracticeExamAnswerRequest request,
        CancellationToken cancellationToken = default
    );
}
