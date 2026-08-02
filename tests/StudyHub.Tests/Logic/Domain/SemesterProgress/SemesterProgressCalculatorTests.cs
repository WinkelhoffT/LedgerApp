using StudyHub.Logic.Domain.SemesterProgress;

namespace StudyHub.Tests.Logic.Domain.SemesterProgress;

public class SemesterProgressCalculatorTests
{
    private static readonly DateOnly StartDate = new(2026, 10, 1);
    private static readonly DateOnly EndDate = new(2027, 3, 31);

    private readonly SemesterProgressCalculator _sut = new();

    [Fact]
    public void Calculate_BeforeSemesterStart_ClampsElapsedToZeroAndRemainingToTotal()
    {
        var result = _sut.Calculate(StartDate, EndDate, StartDate.AddDays(-5));

        Assert.Equal(0, result.ElapsedDays);
        Assert.Equal(result.TotalDays, result.RemainingDays);
        Assert.Equal(0, result.PercentComplete);
    }

    [Fact]
    public void Calculate_OnSemesterStart_CountsTheFirstDayAsElapsed()
    {
        var result = _sut.Calculate(StartDate, EndDate, StartDate);

        Assert.Equal(1, result.ElapsedDays);
        Assert.True(result.PercentComplete > 0 && result.PercentComplete < 100);
    }

    [Fact]
    public void Calculate_DuringSemester_ComputesPartialProgress()
    {
        // 182 total days (Oct 1 2026 - Mar 31 2027 inclusive); 32 days elapsed (inclusive of Nov 1) by Nov 1.
        var result = _sut.Calculate(StartDate, EndDate, new DateOnly(2026, 11, 1));

        Assert.Equal(182, result.TotalDays);
        Assert.Equal(32, result.ElapsedDays);
        Assert.Equal(150, result.RemainingDays);
        Assert.InRange(result.PercentComplete, 0, 100);
        Assert.True(result.PercentComplete > 0 && result.PercentComplete < 100);
    }

    [Fact]
    public void Calculate_OnSemesterEnd_HasFullyElapsedDays()
    {
        var result = _sut.Calculate(StartDate, EndDate, EndDate);

        Assert.Equal(result.TotalDays, result.ElapsedDays);
        Assert.Equal(0, result.RemainingDays);
        Assert.Equal(100, result.PercentComplete);
    }

    [Fact]
    public void Calculate_AfterSemesterEnd_ClampsElapsedToTotalAndRemainingToZero()
    {
        var result = _sut.Calculate(StartDate, EndDate, EndDate.AddDays(10));

        Assert.Equal(result.TotalDays, result.ElapsedDays);
        Assert.Equal(0, result.RemainingDays);
        Assert.Equal(100, result.PercentComplete);
    }

    [Fact]
    public void Calculate_SingleDaySemester_TotalDaysIsOne()
    {
        var singleDay = new DateOnly(2026, 5, 1);

        var result = _sut.Calculate(singleDay, singleDay, singleDay);

        Assert.Equal(1, result.TotalDays);
        Assert.Equal(100, result.PercentComplete);
    }
}
