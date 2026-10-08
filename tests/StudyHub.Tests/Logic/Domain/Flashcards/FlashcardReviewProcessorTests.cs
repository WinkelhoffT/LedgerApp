using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Flashcards;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Domain.Flashcards;

public class FlashcardReviewProcessorTests
{
    // 2026-10-08 12:00 in Berlin (CEST); the study day runs from 02:00 UTC to 02:00 UTC.
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TodayStart = new(2026, 10, 8, 2, 0, 0, DateTimeKind.Utc);

    private readonly StudyDayProvider _studyDayProvider = new(
        new FlashcardStudyOptions { TimeZone = "Europe/Berlin", DayStartHour = 4 },
        new FixedTimeProvider(Now));

    private readonly FlashcardReviewProcessor _sut;
    private readonly StudyDay _today;

    public FlashcardReviewProcessorTests()
    {
        _sut = new FlashcardReviewProcessor(_studyDayProvider);
        _today = _studyDayProvider.GetCurrent();
    }

    private static Flashcard Card(
        FlashcardState state,
        int step = 0,
        int intervalDays = 0,
        int easeFactor = 2500,
        DateTime? dueAt = null,
        int lapses = 0) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Q", "A", null, null, state, step, dueAt ?? Now, intervalDays, easeFactor, 3, lapses, null, Now, Now);

    private DateTime DayStart(int daysFromToday) => _studyDayProvider.GetStart(_today.Date.AddDays(daysFromToday));

    [Theory]
    [InlineData(FlashcardRating.Again, 0, 60)]
    [InlineData(FlashcardRating.Hard, 0, 330)]
    [InlineData(FlashcardRating.Good, 1, 600)]
    public void Answer_NewCard_MovesIntoLearningSteps(FlashcardRating rating, int expectedStep, int expectedDelaySeconds)
    {
        var result = _sut.Answer(Card(FlashcardState.New), rating, _today).Card;

        Assert.Equal(FlashcardState.Learning, result.State);
        Assert.Equal(expectedStep, result.Step);
        Assert.Equal(Now.AddSeconds(expectedDelaySeconds), result.DueAt);
        Assert.Equal(2500, result.EaseFactor);
    }

    [Fact]
    public void Answer_NewCardEasy_GraduatesWithFourDays()
    {
        var result = _sut.Answer(Card(FlashcardState.New), FlashcardRating.Easy, _today).Card;

        Assert.Equal(FlashcardState.Review, result.State);
        Assert.Equal(4, result.IntervalDays);
        Assert.Equal(DayStart(4), result.DueAt);
    }

    [Theory]
    [InlineData(FlashcardRating.Again, 0, 60)]
    [InlineData(FlashcardRating.Hard, 1, 600)]
    public void Answer_LearningCardOnLastStep_RepeatsOrRestartsSteps(FlashcardRating rating, int expectedStep, int expectedDelaySeconds)
    {
        var result = _sut.Answer(Card(FlashcardState.Learning, step: 1), rating, _today).Card;

        Assert.Equal(FlashcardState.Learning, result.State);
        Assert.Equal(expectedStep, result.Step);
        Assert.Equal(Now.AddSeconds(expectedDelaySeconds), result.DueAt);
    }

    [Theory]
    [InlineData(FlashcardRating.Good, 1)]
    [InlineData(FlashcardRating.Easy, 4)]
    public void Answer_LearningCardOnLastStep_Graduates(FlashcardRating rating, int expectedDays)
    {
        var result = _sut.Answer(Card(FlashcardState.Learning, step: 1), rating, _today).Card;

        Assert.Equal(FlashcardState.Review, result.State);
        Assert.Equal(0, result.Step);
        Assert.Equal(expectedDays, result.IntervalDays);
        Assert.Equal(DayStart(expectedDays), result.DueAt);
    }

    [Fact]
    public void Answer_ReviewCardAgain_LapsesIntoRelearning()
    {
        var result = _sut.Answer(Card(FlashcardState.Review, intervalDays: 10, dueAt: TodayStart, lapses: 1), FlashcardRating.Again, _today).Card;

        Assert.Equal(FlashcardState.Relearning, result.State);
        Assert.Equal(0, result.Step);
        Assert.Equal(1, result.IntervalDays);
        Assert.Equal(2300, result.EaseFactor);
        Assert.Equal(2, result.Lapses);
        Assert.Equal(Now.AddMinutes(10), result.DueAt);
    }

    [Theory]
    [InlineData(FlashcardRating.Hard, 12, 2350)]
    [InlineData(FlashcardRating.Good, 25, 2500)]
    [InlineData(FlashcardRating.Easy, 33, 2650)]
    public void Answer_ReviewCardDueToday_UsesSm2Intervals(FlashcardRating rating, int expectedDays, int expectedEase)
    {
        var result = _sut.Answer(Card(FlashcardState.Review, intervalDays: 10, dueAt: TodayStart), rating, _today).Card;

        Assert.Equal(FlashcardState.Review, result.State);
        Assert.Equal(expectedDays, result.IntervalDays);
        Assert.Equal(expectedEase, result.EaseFactor);
        Assert.Equal(DayStart(expectedDays), result.DueAt);
    }

    [Theory]
    [InlineData(FlashcardRating.Hard, 12)]
    [InlineData(FlashcardRating.Good, 30)]
    [InlineData(FlashcardRating.Easy, 46)]
    public void Answer_OverdueReviewCard_AddsDaysLateToGoodAndEasy(FlashcardRating rating, int expectedDays)
    {
        var dueFourDaysAgo = DayStart(-4);

        var result = _sut.Answer(Card(FlashcardState.Review, intervalDays: 10, dueAt: dueFourDaysAgo), rating, _today).Card;

        Assert.Equal(expectedDays, result.IntervalDays);
    }

    [Theory]
    [InlineData(FlashcardRating.Again)]
    [InlineData(FlashcardRating.Hard)]
    public void Answer_ReviewCardAtMinimumEase_KeepsEaseAt130Percent(FlashcardRating rating)
    {
        var result = _sut.Answer(Card(FlashcardState.Review, intervalDays: 10, easeFactor: 1300, dueAt: TodayStart), rating, _today).Card;

        Assert.Equal(1300, result.EaseFactor);
    }

    [Fact]
    public void Answer_ReviewCardWithShortIntervalAndLowEase_KeepsEachButtonAtLeastOneDayApart()
    {
        var card = Card(FlashcardState.Review, intervalDays: 1, easeFactor: 1300, dueAt: TodayStart);

        var intervals = new[] { FlashcardRating.Hard, FlashcardRating.Good, FlashcardRating.Easy }
            .Select(rating => _sut.Answer(card, rating, _today).Card.IntervalDays);

        Assert.Equal([2, 3, 4], intervals);
    }

    [Theory]
    [InlineData(FlashcardRating.Good)]
    [InlineData(FlashcardRating.Easy)]
    public void Answer_ReviewCardWithHugeInterval_CapsAt36500Days(FlashcardRating rating)
    {
        var result = _sut.Answer(Card(FlashcardState.Review, intervalDays: 30_000, dueAt: TodayStart), rating, _today).Card;

        Assert.Equal(36_500, result.IntervalDays);
    }

    [Theory]
    [InlineData(FlashcardRating.Again, 600)]
    [InlineData(FlashcardRating.Hard, 900)]
    public void Answer_RelearningCard_StaysInRelearning(FlashcardRating rating, int expectedDelaySeconds)
    {
        var result = _sut.Answer(Card(FlashcardState.Relearning, intervalDays: 3), rating, _today).Card;

        Assert.Equal(FlashcardState.Relearning, result.State);
        Assert.Equal(Now.AddSeconds(expectedDelaySeconds), result.DueAt);
        Assert.Equal(3, result.IntervalDays);
    }

    [Theory]
    [InlineData(FlashcardRating.Good)]
    [InlineData(FlashcardRating.Easy)]
    public void Answer_RelearningCard_ReturnsToReviewWithLapseInterval(FlashcardRating rating)
    {
        var result = _sut.Answer(Card(FlashcardState.Relearning, intervalDays: 3, easeFactor: 2300), rating, _today).Card;

        Assert.Equal(FlashcardState.Review, result.State);
        Assert.Equal(3, result.IntervalDays);
        Assert.Equal(2300, result.EaseFactor);
        Assert.Equal(DayStart(3), result.DueAt);
    }

    [Fact]
    public void Answer_AcrossTheAutumnClockChange_IsDueAtTheStartOfThatDay()
    {
        var result = _sut.Answer(Card(FlashcardState.Review, intervalDays: 10, dueAt: TodayStart), FlashcardRating.Good, _today).Card;

        // 25 days later is 2026-11-02, in winter time: 04:00 CET is 03:00 UTC.
        Assert.Equal(new DateTime(2026, 11, 2, 3, 0, 0, DateTimeKind.Utc), result.DueAt);
    }

    [Fact]
    public void Answer_RecordsReviewLogAndCountsTheRepetition()
    {
        var card = Card(FlashcardState.Review, intervalDays: 10, dueAt: TodayStart);

        var outcome = _sut.Answer(card, FlashcardRating.Good, _today);

        Assert.Equal(card.Reps + 1, outcome.Card.Reps);
        Assert.Equal(Now, outcome.Card.LastReviewedAt);
        Assert.Equal(card.Id, outcome.Review.FlashcardId);
        Assert.Equal(Now, outcome.Review.ReviewedAt);
        Assert.Equal(FlashcardRating.Good, outcome.Review.Rating);
        Assert.Equal(FlashcardState.Review, outcome.Review.StateBefore);
        Assert.Equal(10, outcome.Review.IntervalDaysBefore);
        Assert.Equal(25, outcome.Review.IntervalDaysAfter);
        Assert.Equal(2500, outcome.Review.EaseFactorAfter);
    }

    [Fact]
    public void PreviewIntervals_NewCard_MatchesAnkiDefaults()
    {
        var previews = _sut.PreviewIntervals(Card(FlashcardState.New), _today);

        Assert.Equal(
            [
                new FlashcardIntervalPreviewDto(FlashcardRating.Again, TimeSpan.FromMinutes(1)),
                new FlashcardIntervalPreviewDto(FlashcardRating.Hard, TimeSpan.FromMinutes(5.5)),
                new FlashcardIntervalPreviewDto(FlashcardRating.Good, TimeSpan.FromMinutes(10)),
                new FlashcardIntervalPreviewDto(FlashcardRating.Easy, TimeSpan.FromDays(4)),
            ],
            previews);
    }

    [Fact]
    public void PreviewIntervals_ReviewCard_ShowsDays()
    {
        var previews = _sut.PreviewIntervals(Card(FlashcardState.Review, intervalDays: 10, dueAt: TodayStart), _today);

        Assert.Equal(
            [TimeSpan.FromMinutes(10), TimeSpan.FromDays(12), TimeSpan.FromDays(25), TimeSpan.FromDays(33)],
            previews.Select(p => p.Interval));
    }
}
