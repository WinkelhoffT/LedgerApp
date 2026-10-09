using StudyHub.Shared.Ai;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Business.Contract;

public interface IPracticeExamGenerationOrchestrator
{
    IReadOnlyList<AiModelDto> GetAvailableModels();

    /// <summary>Writes a practice exam from a course or a deck with AI and saves it.</summary>
    Task<PracticeExamDto> GenerateAsync(
        GeneratePracticeExamRequest request,
        CancellationToken cancellationToken = default
    );
}
