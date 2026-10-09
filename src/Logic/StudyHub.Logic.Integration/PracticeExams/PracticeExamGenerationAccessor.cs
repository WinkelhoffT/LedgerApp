using System.Net.Http.Json;
using StudyHub.Shared.Ai;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Integration.PracticeExams;

public sealed class PracticeExamGenerationAccessor(HttpClient httpClient)
    : IPracticeExamGenerationAccessor
{
    public async Task<IReadOnlyList<AiModelDto>> GetModelsAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            "api/practice-exams/models",
            cancellationToken
        );
        await PracticeExamProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (
            await response.Content.ReadFromJsonAsync<IReadOnlyList<AiModelDto>>(cancellationToken)
        )!;
    }

    public async Task<PracticeExamDto> GenerateAsync(
        GeneratePracticeExamRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/practice-exams/generate",
            request,
            cancellationToken
        );
        await PracticeExamProblemDetailsMapper.EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<PracticeExamDto>(cancellationToken))!;
    }
}
