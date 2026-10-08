using StudyHub.Logic.Domain;
using StudyHub.Shared.Configuration;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Domain.Flashcards;

public class StudyDayProviderTests
{
    private static StudyDayProvider CreateSut(DateTime utcNow, int dayStartHour = 4) =>
        new(new FlashcardStudyOptions { TimeZone = "Europe/Berlin", DayStartHour = dayStartHour }, new FixedTimeProvider(utcNow));

    private static DateTime Utc(int year, int month, int day, int hour, int minute = 0) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    [Fact]
    public void GetCurrent_InSummerTime_StartsTheDayAtFourBerlinTime()
    {
        var sut = CreateSut(Utc(2026, 10, 8, 10));

        var today = sut.GetCurrent();

        Assert.Equal(new DateOnly(2026, 10, 8), today.Date);
        Assert.Equal(Utc(2026, 10, 8, 2), today.Start);
        Assert.Equal(Utc(2026, 10, 9, 2), today.NextStart);
        Assert.Equal(Utc(2026, 10, 8, 10), today.Now);
    }

    [Fact]
    public void GetDate_BeforeTheStartHour_BelongsToThePreviousDay()
    {
        var sut = CreateSut(Utc(2026, 10, 8, 10));

        Assert.Equal(new DateOnly(2026, 10, 7), sut.GetDate(Utc(2026, 10, 8, 1, 59)));
        Assert.Equal(new DateOnly(2026, 10, 8), sut.GetDate(Utc(2026, 10, 8, 2)));
    }

    [Fact]
    public void GetStart_InWinterTime_IsThreeUtc()
    {
        var sut = CreateSut(Utc(2026, 12, 1, 10));

        Assert.Equal(Utc(2026, 12, 1, 3), sut.GetStart(new DateOnly(2026, 12, 1)));
    }

    [Fact]
    public void GetCurrent_OnTheDayBeforeTheAutumnChange_Lasts25Hours()
    {
        // Clocks go back from 03:00 CEST to 02:00 CET on 2026-10-25.
        var today = CreateSut(Utc(2026, 10, 24, 12)).GetCurrent();

        Assert.Equal(Utc(2026, 10, 24, 2), today.Start);
        Assert.Equal(Utc(2026, 10, 25, 3), today.NextStart);
    }

    [Fact]
    public void GetCurrent_OnTheDayBeforeTheSpringChange_Lasts23Hours()
    {
        // Clocks go forward from 02:00 CET to 03:00 CEST on 2026-03-29.
        var today = CreateSut(Utc(2026, 3, 28, 12)).GetCurrent();

        Assert.Equal(Utc(2026, 3, 28, 3), today.Start);
        Assert.Equal(Utc(2026, 3, 29, 2), today.NextStart);
    }

    [Fact]
    public void GetStart_WithStartHourInsideTheSpringGap_StartsWhenTheClocksHaveJumped()
    {
        var sut = CreateSut(Utc(2026, 3, 29, 12), dayStartHour: 2);

        Assert.Equal(Utc(2026, 3, 29, 1), sut.GetStart(new DateOnly(2026, 3, 29)));
    }

    [Fact]
    public void GetDate_AfterTheAutumnChange_UsesWinterTime()
    {
        var sut = CreateSut(Utc(2026, 10, 25, 12));

        Assert.Equal(new DateOnly(2026, 10, 24), sut.GetDate(Utc(2026, 10, 25, 2, 59)));
        Assert.Equal(new DateOnly(2026, 10, 25), sut.GetDate(Utc(2026, 10, 25, 3)));
    }

    [Fact]
    public void Constructor_WithInvalidStartHour_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSut(Utc(2026, 10, 8, 10), dayStartHour: 24));
    }
}
