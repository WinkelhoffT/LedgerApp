using System.Net.Http.Json;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Integration.PracticeExams;

public sealed class PracticeExamAccessor(HttpClient httpClient) : IPracticeExamAccessor
{
    public async Task<IReadOnlyList<PracticeExamDto>> GetAllAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            $"api/practice-exams?includeArchived={includeArchived}",
            cancellationToken
        );
        await PracticeExamProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (
            await response.Content.ReadFromJsonAsync<IReadOnlyList<PracticeExamDto>>(
                cancellationToken
            )
        )!;
    }

    public async Task<PracticeExamDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            $"api/practice-exams/{id}",
            cancellationToken
        );
        await PracticeExamProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<PracticeExamDto>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<PracticeExamAttemptSummaryDto>> GetAttemptsAsync(
        Guid examId,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            $"api/practice-exams/{examId}/attempts",
            cancellationToken
        );
        await PracticeExamProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (
            await response.Content.ReadFromJsonAsync<IReadOnlyList<PracticeExamAttemptSummaryDto>>(
                cancellationToken
            )
        )!;
    }

    public Task<PracticeExamDto> ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) => PostAsync($"api/practice-exams/{id}/archive", cancellationToken);

    public Task<PracticeExamDto> RestoreAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) => PostAsync($"api/practice-exams/{id}/restore", cancellationToken);

    public Task<PracticeExamDto> ExcludeTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default
    ) => PostAsync($"api/practice-exams/tasks/{taskId}/exclude", cancellationToken);

    private async Task<PracticeExamDto> PostAsync(string uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(uri, content: null, cancellationToken);
        await PracticeExamProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<PracticeExamDto>(cancellationToken))!;
    }
}
