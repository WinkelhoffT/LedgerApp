using System.Diagnostics.CodeAnalysis;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain;

public sealed class FlashcardValidator : IFlashcardValidator
{
    public void ValidateGenerationOptions(int cardCount, string? focusHint)
    {
        if (
            cardCount
            is < GenerateFlashcardsRequest.MinCardCount
                or > GenerateFlashcardsRequest.MaxCardCount
        )
        {
            throw new FlashcardValidationException(
                $"Card count must be between {GenerateFlashcardsRequest.MinCardCount} and {GenerateFlashcardsRequest.MaxCardCount}."
            );
        }

        if (focusHint is { Length: > GenerateFlashcardsRequest.FocusHintMaxLength })
        {
            throw new FlashcardValidationException(
                $"Focus hint must not exceed {GenerateFlashcardsRequest.FocusHintMaxLength} characters."
            );
        }
    }

    public IReadOnlyList<FlashcardDto> FilterGeneratedCards(
        IReadOnlyList<FlashcardDto> cards,
        int maxCount
    ) => cards.Select(Normalize).Where(card => GetError(card) is null).Take(maxCount).ToList();

    public IReadOnlyList<FlashcardDto> ValidateCards(IReadOnlyList<FlashcardDto> cards)
    {
        if (cards is null || cards.Count == 0)
        {
            throw new FlashcardValidationException("At least one card is required.");
        }

        if (cards.Count > AddFlashcardsRequest.MaxCardCount)
        {
            throw new FlashcardValidationException(
                $"At most {AddFlashcardsRequest.MaxCardCount} cards can be saved at once."
            );
        }

        var normalized = new List<FlashcardDto>(cards.Count);
        for (var i = 0; i < cards.Count; i++)
        {
            var card = Normalize(cards[i]);
            if (GetError(card) is { } error)
            {
                throw new FlashcardValidationException($"Card {i + 1}: {error}");
            }

            normalized.Add(card);
        }

        return normalized;
    }

    public bool TryNormalize(
        FlashcardDto card,
        out FlashcardDto normalized,
        [NotNullWhen(false)] out string? error
    )
    {
        normalized = Normalize(card);
        error = GetError(normalized);
        return error is null;
    }

    private static FlashcardDto Normalize(FlashcardDto card) =>
        new(
            card?.Front?.Trim() ?? string.Empty,
            card?.Back?.Trim() ?? string.Empty,
            (card?.Tags ?? [])
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        );

    private static string? GetError(FlashcardDto card)
    {
        if (card.Front.Length == 0)
        {
            return "Front is required.";
        }

        if (card.Front.Length > FlashcardDto.FrontMaxLength)
        {
            return $"Front must not exceed {FlashcardDto.FrontMaxLength} characters.";
        }

        if (card.Back.Length == 0)
        {
            return "Back is required.";
        }

        if (card.Back.Length > FlashcardDto.BackMaxLength)
        {
            return $"Back must not exceed {FlashcardDto.BackMaxLength} characters.";
        }

        if (card.Tags.Count > FlashcardDto.MaxTagsPerCard)
        {
            return $"A card must not have more than {FlashcardDto.MaxTagsPerCard} tags.";
        }

        if (card.Tags.Any(tag => tag.Length > FlashcardDto.TagMaxLength))
        {
            return $"Tags must not exceed {FlashcardDto.TagMaxLength} characters.";
        }

        return null;
    }
}
