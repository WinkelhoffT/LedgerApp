using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Configuration;

namespace StudyHub.Logic.Domain;

public sealed class StudyDayProvider : IStudyDayProvider
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;
    private readonly int _dayStartHour;

    public StudyDayProvider(FlashcardStudyOptions options, TimeProvider timeProvider)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(options.DayStartHour);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.DayStartHour, 23);

        _timeProvider = timeProvider;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);
        _dayStartHour = options.DayStartHour;
    }

    public StudyDay GetCurrent()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var date = GetDate(now);

        return new StudyDay(now, date, GetStart(date), GetStart(date.AddDays(1)));
    }

    public DateOnly GetDate(DateTime utc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            _timeZone
        );

        // Wall-clock arithmetic: before the start hour, the time still belongs to the previous day.
        return DateOnly.FromDateTime(local.AddHours(-_dayStartHour));
    }

    public DateTime GetStart(DateOnly date)
    {
        var local = date.ToDateTime(new TimeOnly(_dayStartHour, 0));

        // A start hour inside a daylight-saving gap (e.g. 02:00 on the spring change in Europe)
        // does not exist that day; the day then starts when the clocks have jumped forward.
        if (_timeZone.IsInvalidTime(local))
        {
            local = local.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, _timeZone);
    }
}
