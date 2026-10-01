using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Domain rules for creating and changing a <see cref="Semester"/>. Semesters are immutable
/// records, so every operation returns a new instance instead of mutating the given one.
/// </summary>
public interface ISemesterLifecycle
{
    Semester Create(string name, DateOnly startDate, DateOnly endDate);

    /// <exception cref="SemesterArchivedException">The semester is archived.</exception>
    Semester Update(Semester semester, string name, DateOnly startDate, DateOnly endDate);

    Semester Archive(Semester semester);

    Semester Restore(Semester semester);
}
