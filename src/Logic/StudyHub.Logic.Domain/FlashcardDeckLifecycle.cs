using System.Text.RegularExpressions;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain;

public sealed partial class FlashcardDeckLifecycle(TimeProvider timeProvider)
    : IFlashcardDeckLifecycle
{
    public FlashcardDeck Create(
        string name,
        Guid? courseId,
        Guid? semesterId,
        int newCardsPerDay,
        int reviewsPerDay
    )
    {
        ValidateOwner(courseId, semesterId);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new FlashcardDeck(
            Id: Guid.CreateVersion7(),
            Name: NormalizeName(name),
            CourseId: courseId,
            SemesterId: semesterId,
            NewCardsPerDay: ValidateLimit(newCardsPerDay, "New cards per day"),
            ReviewsPerDay: ValidateLimit(reviewsPerDay, "Reviews per day"),
            IsArchived: false,
            CreatedAt: now,
            UpdatedAt: now
        );
    }

    public FlashcardDeck Update(
        FlashcardDeck deck,
        string name,
        Guid? courseId,
        Guid? semesterId,
        int newCardsPerDay,
        int reviewsPerDay
    )
    {
        if (deck.IsArchived)
        {
            throw new FlashcardDeckArchivedException(deck.Id);
        }

        ValidateOwner(courseId, semesterId);

        return deck with
        {
            Name = NormalizeName(name),
            CourseId = courseId,
            SemesterId = semesterId,
            NewCardsPerDay = ValidateLimit(newCardsPerDay, "New cards per day"),
            ReviewsPerDay = ValidateLimit(reviewsPerDay, "Reviews per day"),
            UpdatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
    }

    public FlashcardDeck Archive(FlashcardDeck deck) =>
        deck.IsArchived
            ? deck
            : deck with
            {
                IsArchived = true,
                UpdatedAt = timeProvider.GetUtcNow().UtcDateTime,
            };

    public FlashcardDeck Restore(FlashcardDeck deck) =>
        !deck.IsArchived
            ? deck
            : deck with
            {
                IsArchived = false,
                UpdatedAt = timeProvider.GetUtcNow().UtcDateTime,
            };

    public string NormalizeName(string name)
    {
        var normalized = Whitespace().Replace(name ?? string.Empty, " ").Trim();
        if (normalized.Length == 0)
        {
            throw new FlashcardValidationException("Deck name is required.");
        }

        if (normalized.Length > FlashcardDeck.NameMaxLength)
        {
            throw new FlashcardValidationException(
                $"Deck name must not exceed {FlashcardDeck.NameMaxLength} characters."
            );
        }

        return normalized;
    }

    private static void ValidateOwner(Guid? courseId, Guid? semesterId)
    {
        if (courseId is not null && semesterId is not null)
        {
            throw new FlashcardValidationException(
                "A deck can belong to a course or a semester, not both."
            );
        }
    }

    private static int ValidateLimit(int value, string label)
    {
        if (value is < 0 or > FlashcardDeck.MaxCardsPerDay)
        {
            throw new FlashcardValidationException(
                $"{label} must be between 0 and {FlashcardDeck.MaxCardsPerDay}."
            );
        }

        return value;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
