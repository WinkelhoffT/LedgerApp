using StudyHub.Logic.Domain.Contract.Semesters;
using StudyHub.Shared.Domain.Semesters;

namespace StudyHub.Logic.Domain.Semesters;

public sealed class ActiveSemesterProvider : IActiveSemesterProvider
{
    public Semester? GetActive(IReadOnlyList<Semester> semesters, DateOnly today) =>
        semesters
            .Where(s => !s.IsArchived && s.StartDate <= today && today <= s.EndDate)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();
}
