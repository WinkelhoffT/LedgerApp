using System.Net.Http.Json;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Integration.PracticeExams;

public sealed class PracticeExamAttemptAccessor(HttpClient httpClient)
    : IPracticeExamAttemptAccessor
{
    public async Task<PracticeExamSheetDto> StartAsync(
        Guid examId,
        StartPracticeExamAttemptRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/practice-exams/{examId}/attempts",
            request,
            cancellationToken
        );
        await PracticeExamProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<PracticeExamSheetDto>(cancellationToken))!;
    }

    public async Task<PracticeExamSheetDto> GetSheetAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            $"api/practice-exam-attempts/{attemptId}/sheet",
            cancellationToken
        );
        await PracticeExamProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<PracticeExamSheetDto>(cancellationToken))!;
    }

    public async Task SaveAnswerAsync(
        Guid attemptId,
        Guid taskId,
        SavePracticeExamAnswerRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/practice-exam-attempts/{attemptId}/answers/{taskId}",
            request,
            cancellationToken
        );
        await PracticeExamProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<PracticeExamReviewDto> SubmitAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.PostAsync(
            $"api/practice-exam-attempts/{attemptId}/submit",
            content: null,
            cancellationToken
        );
        await PracticeExamProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (
            await response.Content.ReadFromJsonAsync<PracticeExamReviewDto>(cancellationToken)
        )!;
    }

    public async Task<PracticeExamReviewDto> GetReviewAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            $"api/practice-exam-attempts/{attemptId}/review",
            cancellationToken
        );
        await PracticeExamProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (
            await response.Content.ReadFromJsonAsync<PracticeExamReviewDto>(cancellationToken)
        )!;
    }

    public async Task<PracticeExamReviewDto> GradeAsync(
        Guid attemptId,
        Guid taskId,
        GradePracticeExamAnswerRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/practice-exam-attempts/{attemptId}/answers/{taskId}/grading",
            request,
            cancellationToken
        );
        await PracticeExamProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (
            await response.Content.ReadFromJsonAsync<PracticeExamReviewDto>(cancellationToken)
        )!;
    }
}
