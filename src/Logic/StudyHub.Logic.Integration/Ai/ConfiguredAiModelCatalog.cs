using Microsoft.Extensions.Options;
using StudyHub.Shared.Ai;
using StudyHub.Shared.Configuration;

namespace StudyHub.Logic.Integration.Ai;

/// <summary>
/// Reads the selectable models from <see cref="AnthropicOptions.Models"/>. The default model is
/// always part of the list, even when it isn't configured there explicitly.
/// </summary>
public sealed class ConfiguredAiModelCatalog(IOptions<AnthropicOptions> options) : IAiModelCatalog
{
    public string DefaultModelId => options.Value.DefaultModel.Trim();

    public IReadOnlyList<AiModelDto> GetModels()
    {
        var defaultModelId = DefaultModelId;

        var models = options.Value.Models
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .Select(m => (Id: m.Id.Trim(), DisplayName: string.IsNullOrWhiteSpace(m.DisplayName) ? m.Id.Trim() : m.DisplayName.Trim()))
            .DistinctBy(m => m.Id, StringComparer.Ordinal)
            .ToList();

        if (!models.Any(m => m.Id == defaultModelId))
        {
            models.Insert(0, (defaultModelId, defaultModelId));
        }

        return models.Select(m => new AiModelDto(m.Id, m.DisplayName, m.Id == defaultModelId)).ToList();
    }

    public bool IsAvailable(string modelId) => GetModels().Any(m => m.Id == modelId);
}
