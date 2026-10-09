using StudyHub.Shared.Ai;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Integration.PracticeExams;

/// <summary>Narrow HTTP access to StudyHub.Api's practice exam generation, covering what the generator page calls.</summary>
public interface IPracticeExamGenerationAccessor
{
    Task<IReadOnlyList<AiModelDto>> GetModelsAsync(CancellationToken cancellationToken = default);

    Task<PracticeExamDto> GenerateAsync(
        GeneratePracticeExamRequest request,
        CancellationToken cancellationToken = default
    );
}
