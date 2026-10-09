using StudyHub.Shared.Ai;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Integration.Ai;

/// <summary>Writes a practice exam from numbered notes or cards through an AI provider.</summary>
public interface IPracticeExamGenerator
{
    /// <summary>The version of the prompt the generator uses, stored with every exam.</summary>
    string PromptVersion { get; }

    /// <exception cref="AiNotConfiguredException">No API key is configured.</exception>
    /// <exception cref="AiGenerationFailedException">The provider failed, refused, or returned unusable output.</exception>
    Task<IReadOnlyList<GeneratedPracticeExamTask>> GenerateAsync(
        PracticeExamGenerationInput input,
        CancellationToken cancellationToken = default
    );
}
