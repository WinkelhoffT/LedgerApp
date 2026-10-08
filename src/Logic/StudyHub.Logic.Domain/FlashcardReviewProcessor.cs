using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain;

/// <summary>
/// Anki's SM-2 scheduler with Anki's default deck options as fixed values (see
/// <c>docs/plans/flashcard-study-plan.md</c>, section 3.2). Intervals are rounded to whole days the
/// way Anki's current scheduler does it, and no fuzz is applied, so every result is deterministic.
/// </summary>
public sealed class FlashcardReviewProcessor(IStudyDayProvider studyDayProvider) : IFlashcardReviewProcessor
{
    public const int StartingEaseFactor = 2500;
    private const int MinimumEaseFactor = 1300;
    private const int AgainEasePenalty = 200;
    private const int HardEasePenalty = 150;
    private const int EasyEaseBonus = 150;
    private const double HardMultiplier = 1.2;
    private const double EasyBonus = 1.3;
    private const int GraduatingIntervalDays = 1;
    private const int EasyIntervalDays = 4;
    private const int MinimumLapseIntervalDays = 1;
    private const int MaximumIntervalDays = 36_500;

    private static readonly TimeSpan[] LearningSteps = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10)];
    private static readonly TimeSpan[] RelearningSteps = [TimeSpan.FromMinutes(10)];

    public FlashcardReviewOutcome Answer(Flashcard card, FlashcardRating rating, StudyDay today)
    {
        var answer = Schedule(card, rating, today);

        var updated = card with
        {
            State = answer.State,
            Step = answer.Step,
            DueAt = answer.DueAt,
            IntervalDays = answer.IntervalDays,
            EaseFactor = answer.EaseFactor,
            Lapses = answer.Lapses,
            Reps = card.Reps + 1,
            LastReviewedAt = today.Now,
            UpdatedAt = today.Now,
        };

        var review = new FlashcardReview(
            Id: Guid.CreateVersion7(),
            FlashcardId: card.Id,
            ReviewedAt: today.Now,
            Rating: rating,
            StateBefore: card.State,
            IntervalDaysBefore: card.IntervalDays,
            IntervalDaysAfter: updated.IntervalDays,
            EaseFactorAfter: updated.EaseFactor);

        return new FlashcardReviewOutcome(updated, review);
    }

    public IReadOnlyList<FlashcardIntervalPreviewDto> PreviewIntervals(Flashcard card, StudyDay today) =>
        Enum.GetValues<FlashcardRating>()
            .Select(rating => new FlashcardIntervalPreviewDto(rating, Schedule(card, rating, today).Interval))
            .ToList();

    private ScheduledAnswer Schedule(Flashcard card, FlashcardRating rating, StudyDay today) =>
        card.State switch
        {
            FlashcardState.New => ScheduleSteps(card, FlashcardState.Learning, LearningSteps, 0, rating, today, GraduatingIntervalDays, EasyIntervalDays),
            FlashcardState.Learning => ScheduleSteps(card, FlashcardState.Learning, LearningSteps, card.Step, rating, today, GraduatingIntervalDays, EasyIntervalDays),
            FlashcardState.Relearning => ScheduleSteps(card, FlashcardState.Relearning, RelearningSteps, card.Step, rating, today, card.IntervalDays, card.IntervalDays),
            FlashcardState.Review => ScheduleReview(card, rating, today),
            _ => throw new ArgumentOutOfRangeException(nameof(card), card.State, "Unknown card state."),
        };

    // Learning and relearning share one rule set; they differ in their steps and in the interval a
    // card leaves them with (1 or 4 days from learning, the interval set at the lapse from relearning).
    private ScheduledAnswer ScheduleSteps(
        Flashcard card,
        FlashcardState state,
        TimeSpan[] steps,
        int currentStep,
        FlashcardRating rating,
        StudyDay today,
        int goodIntervalDays,
        int easyIntervalDays)
    {
        var step = Math.Clamp(currentStep, 0, steps.Length - 1);

        return rating switch
        {
            FlashcardRating.Again => InSteps(card, state, 0, steps[0], today),
            FlashcardRating.Hard => InSteps(card, state, step, HardDelay(steps, step), today),
            FlashcardRating.Good when step + 1 < steps.Length => InSteps(card, state, step + 1, steps[step + 1], today),
            FlashcardRating.Good => ToReview(card, goodIntervalDays, card.EaseFactor, card.Lapses, today),
            FlashcardRating.Easy => ToReview(card, easyIntervalDays, card.EaseFactor, card.Lapses, today),
            _ => throw new ArgumentOutOfRangeException(nameof(rating), rating, "Unknown rating."),
        };
    }

    private ScheduledAnswer ScheduleReview(Flashcard card, FlashcardRating rating, StudyDay today)
    {
        var interval = Math.Max(1, card.IntervalDays);
        var ease = card.EaseFactor / 1000.0;
        var daysLate = Math.Max(0, today.Date.DayNumber - studyDayProvider.GetDate(card.DueAt).DayNumber);

        var hard = Constrain(interval * HardMultiplier, interval + 1);
        var good = Constrain((interval + daysLate / 2.0) * ease, hard + 1);
        var easy = Constrain((interval + daysLate) * ease * EasyBonus, good + 1);

        return rating switch
        {
            FlashcardRating.Again => new ScheduledAnswer(
                FlashcardState.Relearning,
                0,
                MinimumLapseIntervalDays,
                Math.Max(MinimumEaseFactor, card.EaseFactor - AgainEasePenalty),
                card.Lapses + 1,
                today.Now + RelearningSteps[0],
                RelearningSteps[0]),
            FlashcardRating.Hard => ToReview(card, hard, Math.Max(MinimumEaseFactor, card.EaseFactor - HardEasePenalty), card.Lapses, today),
            FlashcardRating.Good => ToReview(card, good, card.EaseFactor, card.Lapses, today),
            FlashcardRating.Easy => ToReview(card, easy, card.EaseFactor + EasyEaseBonus, card.Lapses, today),
            _ => throw new ArgumentOutOfRangeException(nameof(rating), rating, "Unknown rating."),
        };
    }

    private static ScheduledAnswer InSteps(Flashcard card, FlashcardState state, int step, TimeSpan delay, StudyDay today) =>
        new(state, step, card.IntervalDays, card.EaseFactor, card.Lapses, today.Now + delay, delay);

    // A review card is due from the start of its due day, not at the time of day it was answered.
    private ScheduledAnswer ToReview(Flashcard card, int intervalDays, int easeFactor, int lapses, StudyDay today)
    {
        var days = Math.Clamp(intervalDays, 1, MaximumIntervalDays);
        return new ScheduledAnswer(
            FlashcardState.Review,
            0,
            days,
            easeFactor,
            lapses,
            studyDayProvider.GetStart(today.Date.AddDays(days)),
            TimeSpan.FromDays(days));
    }

    // Hard on the first step waits halfway between the first two steps (1.5x the step if there is
    // only one); on a later step it repeats that step.
    private static TimeSpan HardDelay(TimeSpan[] steps, int step) =>
        step > 0 ? steps[step]
        : steps.Length > 1 ? (steps[0] + steps[1]) / 2
        : steps[0] * 1.5;

    private static int Constrain(double days, int minimum) =>
        Math.Min(MaximumIntervalDays, Math.Max(minimum, (int)Math.Round(days, MidpointRounding.AwayFromZero)));
}
