using System.Text.RegularExpressions;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain;

public sealed partial class FlashcardLifecycle(
    IFlashcardValidator flashcardValidator,
    TimeProvider timeProvider
) : IFlashcardLifecycle
{
    public IReadOnlyList<Flashcard> Create(
        Guid deckId,
        IReadOnlyList<FlashcardDto> cards,
        Guid? sourceNoteId
    )
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var created = new List<Flashcard>(cards.Count);

        for (var i = 0; i < cards.Count; i++)
        {
            var content = Validate(cards[i], cards.Count > 1 ? $"Card {i + 1}: " : string.Empty);

            // A new card's DueAt is its queue position. Cards created together share one timestamp,
            // so each gets one more tick to keep the order they were given in.
            created.Add(
                new Flashcard(
                    Id: Guid.CreateVersion7(),
                    DeckId: deckId,
                    Front: content.Front,
                    Back: content.Back,
                    Tags: FormatTags(content.Tags),
                    SourceNoteId: sourceNoteId,
                    State: FlashcardState.New,
                    Step: 0,
                    DueAt: now.AddTicks(i),
                    IntervalDays: 0,
                    EaseFactor: FlashcardReviewProcessor.StartingEaseFactor,
                    Reps: 0,
                    Lapses: 0,
                    LastReviewedAt: null,
                    CreatedAt: now,
                    UpdatedAt: now
                )
            );
        }

        return created;
    }

    public Flashcard UpdateContent(Flashcard card, FlashcardDto content)
    {
        var validated = Validate(content, string.Empty);

        return card with
        {
            Front = validated.Front,
            Back = validated.Back,
            Tags = FormatTags(validated.Tags),
            UpdatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
    }

    private FlashcardDto Validate(FlashcardDto card, string errorPrefix) =>
        flashcardValidator.TryNormalize(card, out var normalized, out var error)
            ? normalized
            : throw new FlashcardValidationException(errorPrefix + error);

    // Anki separates tags with spaces, so whitespace inside a single tag becomes an underscore.
    private static string? FormatTags(IReadOnlyList<string> tags)
    {
        var formatted = tags.Select(tag => Whitespace().Replace(tag, "_"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return formatted.Count == 0 ? null : string.Join(' ', formatted);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
