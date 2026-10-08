using System.Diagnostics.CodeAnalysis;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Domain rules for flashcards: allowed generation options and what makes a card valid.
/// Every method returns normalized copies (trimmed text, cleaned-up tags) instead of mutating input.
/// </summary>
public interface IFlashcardValidator
{
    /// <exception cref="FlashcardValidationException">Card count or focus hint is out of range.</exception>
    void ValidateGenerationOptions(int cardCount, string? focusHint);

    /// <summary>
    /// Normalizes AI-generated cards, drops the ones that break a card rule, and caps the result at
    /// <paramref name="maxCount"/>. AI output is untrusted, so an invalid card is skipped rather than
    /// failing the whole generation.
    /// </summary>
    IReadOnlyList<FlashcardDto> FilterGeneratedCards(IReadOnlyList<FlashcardDto> cards, int maxCount);

    /// <summary>Normalizes user-edited cards before they are saved.</summary>
    /// <exception cref="FlashcardValidationException">No cards, too many cards, or a card breaks a rule.</exception>
    IReadOnlyList<FlashcardDto> ValidateCards(IReadOnlyList<FlashcardDto> cards);

    /// <summary>Normalizes one card; <paramref name="error"/> names the first rule it breaks.</summary>
    bool TryNormalize(FlashcardDto card, out FlashcardDto normalized, [NotNullWhen(false)] out string? error);
}
