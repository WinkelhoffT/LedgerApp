namespace StudyHub.Logic.Domain.Contract;

/// <summary>The days a calendar view shows, from <see cref="Start"/> to <see cref="End"/> (both included).</summary>
public sealed record CalendarPeriod(DateOnly Start, DateOnly End)
{
    public IEnumerable<DateOnly> Days
    {
        get
        {
            for (var date = Start; date <= End; date = date.AddDays(1))
            {
                yield return date;
            }
        }
    }
}
