// `SemesterProgress` (the DTO) can't be brought in with a plain `using`: this file's own
// namespace ends in the same segment name (`SemesterProgress`) as the Contract namespace it's
// declared in, which C# resolves as an enclosing-namespace member before it ever consults a
// using-alias, producing a namespace/type ambiguity (CS0118). Fully qualifying it is the reliable
// way around that; the interface doesn't collide the same way; so it's imported normally.
using StudyHub.Logic.Domain.Contract.SemesterProgress;

namespace StudyHub.Logic.Domain.SemesterProgress;

public sealed class SemesterProgressCalculator : ISemesterProgressCalculator
{
    public global::StudyHub.Logic.Domain.Contract.SemesterProgress.SemesterProgress Calculate(DateOnly startDate, DateOnly endDate, DateOnly today)
    {
        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
        var elapsedDays = Math.Clamp(today.DayNumber - startDate.DayNumber + 1, 0, totalDays);
        var remainingDays = totalDays - elapsedDays;
        var percentComplete = Math.Clamp(elapsedDays / (double)totalDays * 100, 0, 100);

        return new global::StudyHub.Logic.Domain.Contract.SemesterProgress.SemesterProgress(totalDays, elapsedDays, remainingDays, percentComplete);
    }
}
