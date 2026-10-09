using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;

namespace StudyHub.Tests.Logic.Domain.Analytics;

public class StudyStreakProviderTests
{
    private static readonly DateOnly Today = new(2026, 10, 9);

    private readonly StudyStreakProvider _sut = new();

    private static IEnumerable<DateOnly> Days(params int[] daysAgo) =>
        daysAgo.Select(d => Today.AddDays(-d));

    [Fact]
    public void GetStreak_WithoutStudyDays_IsZero()
    {
        Assert.Equal(new StudyStreak(0, 0), _sut.GetStreak([], Today));
    }

    [Fact]
    public void GetStreak_StudiedToday_CountsTodayAndTheDaysBefore()
    {
        Assert.Equal(new StudyStreak(3, 3), _sut.GetStreak(Days(0, 1, 2, 4), Today));
    }

    [Fact]
    public void GetStreak_NotYetStudiedToday_ContinuesFromYesterday()
    {
        Assert.Equal(new StudyStreak(2, 2), _sut.GetStreak(Days(1, 2), Today));
    }

    [Fact]
    public void GetStreak_AMissedDay_BreaksTheCurrentStreak()
    {
        Assert.Equal(new StudyStreak(0, 3), _sut.GetStreak(Days(2, 3, 4), Today));
    }

    [Fact]
    public void GetStreak_LongestRun_CanLieInThePast()
    {
        Assert.Equal(new StudyStreak(1, 4), _sut.GetStreak(Days(0, 5, 6, 7, 8, 10), Today));
    }

    [Fact]
    public void GetStreak_IgnoresDaysAfterToday()
    {
        Assert.Equal(
            new StudyStreak(1, 1),
            _sut.GetStreak([Today, Today.AddDays(1), Today.AddDays(2)], Today)
        );
    }

    [Fact]
    public void GetStreak_AsLongAsTheWindow_CountsEveryDay()
    {
        Assert.Equal(
            new StudyStreak(365, 365),
            _sut.GetStreak(Days(Enumerable.Range(0, 365).ToArray()), Today)
        );
    }
}
