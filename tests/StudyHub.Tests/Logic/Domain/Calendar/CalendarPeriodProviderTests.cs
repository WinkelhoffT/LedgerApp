using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Configuration;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Domain.Calendar;

public class CalendarPeriodProviderTests
{
    private static CalendarPeriodProvider CreateSut(DateTime utcNow) =>
        new(new CalendarOptions { TimeZone = "Europe/Berlin" }, new FixedTimeProvider(utcNow));

    private static readonly CalendarPeriodProvider Sut = CreateSut(
        new DateTime(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc)
    );

    private static CalendarTimeSlot Slot(int hour, int minute, int durationMinutes) =>
        new(Guid.NewGuid(), new TimeOnly(hour, minute), durationMinutes);

    [Fact]
    public void GetMonth_StartingOnMondayWith28Days_ShowsExactlyFourWeeks()
    {
        var period = Sut.GetMonth(2027, 2);

        Assert.Equal(
            new CalendarPeriod(new DateOnly(2027, 2, 1), new DateOnly(2027, 2, 28)),
            period
        );
        Assert.Equal(28, period.Days.Count());
    }

    [Fact]
    public void GetMonth_StartingOnSunday_ShowsSixWeeks()
    {
        var period = Sut.GetMonth(2026, 11);

        Assert.Equal(
            new CalendarPeriod(new DateOnly(2026, 10, 26), new DateOnly(2026, 12, 6)),
            period
        );
        Assert.Equal(42, period.Days.Count());
    }

    [Fact]
    public void GetMonth_December_EndsInTheNextYear()
    {
        Assert.Equal(
            new CalendarPeriod(new DateOnly(2026, 11, 30), new DateOnly(2027, 1, 3)),
            Sut.GetMonth(2026, 12)
        );
    }

    [Fact]
    public void GetWeek_OfASunday_StartsOnThePreviousMonday()
    {
        var date = new DateOnly(2026, 10, 11);

        Assert.Equal(
            new CalendarPeriod(new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 11)),
            Sut.GetWeek(date)
        );
        Assert.Equal(41, Sut.GetIsoWeek(date));
    }

    [Fact]
    public void GetWeek_OfAMonday_StartsThatDay()
    {
        Assert.Equal(
            new CalendarPeriod(new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 11)),
            Sut.GetWeek(new DateOnly(2026, 10, 5))
        );
    }

    [Fact]
    public void GetWeek_AcrossTheYearBoundary_IsIsoWeek53()
    {
        var date = new DateOnly(2027, 1, 2);

        Assert.Equal(
            new CalendarPeriod(new DateOnly(2026, 12, 28), new DateOnly(2027, 1, 3)),
            Sut.GetWeek(date)
        );
        Assert.Equal(53, Sut.GetIsoWeek(date));
    }

    [Theory]
    [InlineData(2026, 10, 7, 21, 59, 2026, 10, 7)]
    [InlineData(2026, 10, 7, 22, 0, 2026, 10, 8)]
    [InlineData(2026, 10, 24, 22, 0, 2026, 10, 25)]
    [InlineData(2026, 10, 25, 22, 59, 2026, 10, 25)]
    [InlineData(2026, 10, 25, 23, 0, 2026, 10, 26)]
    public void GetToday_UsesTheConfiguredTimeZoneAcrossDaylightSaving(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int expectedYear,
        int expectedMonth,
        int expectedDay
    )
    {
        // Berlin is UTC+2 until 2026-10-25 03:00 local time and UTC+1 afterwards.
        var sut = CreateSut(new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc));

        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), sut.GetToday());
    }

    [Fact]
    public void GetWeekHours_WithoutSlots_Is7To22()
    {
        Assert.Equal(new CalendarHourRange(7, 22), Sut.GetWeekHours([]));
    }

    [Fact]
    public void GetWeekHours_WidensToTheFullHourAroundEarlyAndLateSlots()
    {
        Assert.Equal(
            new CalendarHourRange(6, 23),
            Sut.GetWeekHours([Slot(6, 30, 60), Slot(21, 30, 45)])
        );
    }

    [Fact]
    public void GetWeekHours_WithSlotEndingOnTheHour_DoesNotAddAnHour()
    {
        Assert.Equal(
            new CalendarHourRange(7, 22),
            Sut.GetWeekHours([Slot(7, 0, 60), Slot(21, 0, 60)])
        );
    }

    [Fact]
    public void GetWeekHours_WithSlotEndingAtMidnight_EndsAt24()
    {
        Assert.Equal(new CalendarHourRange(7, 24), Sut.GetWeekHours([Slot(23, 0, 60)]));
    }
}
