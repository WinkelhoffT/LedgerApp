using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain;

/// <param name="Interval">Time until the card comes back, as shown on the answer button.</param>
internal sealed record ScheduledAnswer(
    FlashcardState State,
    int Step,
    int IntervalDays,
    int EaseFactor,
    int Lapses,
    DateTime DueAt,
    TimeSpan Interval);
