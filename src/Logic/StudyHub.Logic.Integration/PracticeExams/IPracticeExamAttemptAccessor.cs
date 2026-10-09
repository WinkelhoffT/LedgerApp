using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Integration.PracticeExams;

/// <summary>Narrow HTTP access to StudyHub.Api's attempt endpoints, covering what the exam sheet and the review call.</summary>
public interface IPracticeExamAttemptAccessor
{
    Task<PracticeExamSheetDto> StartAsync(
        Guid examId,
        StartPracticeExamAttemptRequest request,
        CancellationToken cancellationToken = default
    );

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

    Task<PracticeExamReviewDto> GetReviewAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default
    );

    Task<PracticeExamReviewDto> GradeAsync(
        Guid attemptId,
        Guid taskId,
        GradePracticeExamAnswerRequest request,
        CancellationToken cancellationToken = default
    );
}
