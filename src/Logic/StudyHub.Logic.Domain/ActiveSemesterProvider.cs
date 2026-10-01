using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Domain;

public sealed class ActiveSemesterProvider : IActiveSemesterProvider
{
    public Semester? GetActive(IReadOnlyList<Semester> semesters, DateOnly today) =>
        semesters
            .Where(s => !s.IsArchived && s.StartDate <= today && today <= s.EndDate)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();
}
