using StudyHub.Shared.Ai;

namespace StudyHub.Logic.Integration.Ai;

/// <summary>The models users may choose for AI features, as configured for StudyHub.Api.</summary>
public interface IAiModelCatalog
{
    string DefaultModelId { get; }

    IReadOnlyList<AiModelDto> GetModels();

    bool IsAvailable(string modelId);
}
