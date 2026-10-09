using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Domain;

public sealed class SemesterLifecycle : ISemesterLifecycle
{
    public Semester Create(string name, DateOnly startDate, DateOnly endDate)
    {
        var now = DateTime.UtcNow;

        return new Semester(
            Id: Guid.NewGuid(),
            Name: ValidateName(name),
            StartDate: startDate,
            EndDate: ValidateEndDate(startDate, endDate),
            IsArchived: false,
            CreatedAt: now,
            UpdatedAt: now
        );
    }

    public Semester Update(Semester semester, string name, DateOnly startDate, DateOnly endDate)
    {
        if (semester.IsArchived)
        {
            throw new SemesterArchivedException(semester.Id);
        }

        return semester with
        {
            Name = ValidateName(name),
            StartDate = startDate,
            EndDate = ValidateEndDate(startDate, endDate),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public Semester Archive(Semester semester) =>
        semester.IsArchived
            ? semester
            : semester with
            {
                IsArchived = true,
                UpdatedAt = DateTime.UtcNow,
            };

    public Semester Restore(Semester semester) =>
        !semester.IsArchived
            ? semester
            : semester with
            {
                IsArchived = false,
                UpdatedAt = DateTime.UtcNow,
            };

    private static string ValidateName(string name)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            throw new SemesterValidationException("Semester name is required.");
        }

        if (trimmedName.Length > CreateSemesterRequest.NameMaxLength)
        {
            throw new SemesterValidationException(
                $"Semester name must not exceed {CreateSemesterRequest.NameMaxLength} characters."
            );
        }

        return trimmedName;
    }

    private static DateOnly ValidateEndDate(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new SemesterValidationException(
                "Semester end date must not be before the start date."
            );
        }

        return endDate;
    }
}
