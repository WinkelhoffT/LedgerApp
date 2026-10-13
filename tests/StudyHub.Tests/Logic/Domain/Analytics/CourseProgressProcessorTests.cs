using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Analytics;

namespace StudyHub.Tests.Logic.Domain.Analytics;

public class CourseProgressProcessorTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid CourseId = Guid.NewGuid();

    private readonly CourseProgressProcessor _sut = new();

    private static FlashcardDeckProgressCounts Deck(
        int newCards,
        int learning,
        int young,
        int mature
    ) => new(Guid.NewGuid(), CourseId, newCards, learning, young, mature);

    private static PracticeExamResult Result(
        int minutesAgo,
        int awardedPoints,
        int maxPoints = 40
    ) => new(CourseId, Now.AddMinutes(-minutesAgo), awardedPoints, maxPoints);

    [Fact]
    public void GetFlashcardProgress_SumsTheBucketsOfAllDecks()
    {
        var progress = _sut.GetFlashcardProgress([Deck(10, 2, 3, 5), Deck(0, 1, 4, 5)]);

        Assert.Equal(new FlashcardProgressDto(10, 3, 7, 10, 30, 57), progress);
    }

    [Theory]
    [InlineData(2, 3, 67)]
    [InlineData(1, 8, 13)]
    [InlineData(1, 2, 50)]
    [InlineData(199, 200, 100)]
    public void GetFlashcardProgress_RoundsTheLearnedPercent(int learned, int total, int expected)
    {
        var progress = _sut.GetFlashcardProgress([Deck(total - learned, 0, learned, 0)]);

        Assert.Equal(expected, progress.LearnedPercent);
    }

    [Fact]
    public void GetFlashcardProgress_WithoutCards_HasNoLearnedPercent()
    {
        Assert.Equal(new FlashcardProgressDto(0, 0, 0, 0, 0, null), _sut.GetFlashcardProgress([]));
    }

    [Fact]
    public void GetExamResults_ReturnsTheLatestAndTheBestResult()
    {
        var results = _sut.GetExamResults([Result(10, 20), Result(30, 36), Result(60, 10)]);

        Assert.Equal(new CourseExamResults(50, 90, 3), results);
    }

    [Fact]
    public void GetExamResults_WithoutGradedAttempts_HasNoResults()
    {
        Assert.Equal(new CourseExamResults(null, null, 0), _sut.GetExamResults([]));
    }

    [Fact]
    public void GetExamResults_RoundsToWholePercent()
    {
        Assert.Equal(new CourseExamResults(67, 67, 1), _sut.GetExamResults([Result(0, 2, 3)]));
    }
}
