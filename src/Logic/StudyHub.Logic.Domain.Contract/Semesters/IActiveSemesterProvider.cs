using StudyHub.Shared.Domain.Semesters;

namespace StudyHub.Logic.Domain.Contract.Semesters;

public interface IActiveSemesterProvider
{
    /// <summary>
    /// Selects the semester that is currently active as of <paramref name="today"/> - not
    /// archived, and with a date range covering <paramref name="today"/>. When several semesters
    /// overlap, the most recently started one wins.
    /// </summary>
    Semester? GetActive(IReadOnlyList<Semester> semesters, DateOnly today);
}
