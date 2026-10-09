using StudyHub.Shared.Ai;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Ai;

/// <summary>Generates flashcards for one Markdown note through an AI provider.</summary>
public interface IFlashcardGenerator
{
    /// <exception cref="AiNotConfiguredException">No API key is configured.</exception>
    /// <exception cref="AiGenerationFailedException">The provider failed, refused, or returned unusable output.</exception>
    Task<IReadOnlyList<FlashcardDto>> GenerateAsync(
        FlashcardGenerationInput input,
        CancellationToken cancellationToken = default
    );
}
