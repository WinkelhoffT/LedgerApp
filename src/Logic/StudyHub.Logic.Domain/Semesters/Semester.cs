namespace StudyHub.Logic.Domain.Semesters;

public sealed class Semester
{
    public const int NameMaxLength = 100;

    private Semester()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static Semester Create(string name, DateOnly startDate, DateOnly endDate)
    {
        var semester = new Semester
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
        };

        semester.SetDetails(name, startDate, endDate);

        return semester;
    }

    public void Update(string name, DateOnly startDate, DateOnly endDate)
    {
        if (IsArchived)
        {
            throw new SemesterArchivedException(Id);
        }

        SetDetails(name, startDate, endDate);
    }

    public void Archive()
    {
        if (IsArchived)
        {
            return;
        }

        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsArchived)
        {
            return;
        }

        IsArchived = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetDetails(string name, DateOnly startDate, DateOnly endDate)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            throw new SemesterValidationException("Semester name is required.");
        }

        if (trimmedName.Length > NameMaxLength)
        {
            throw new SemesterValidationException($"Semester name must not exceed {NameMaxLength} characters.");
        }

        if (endDate < startDate)
        {
            throw new SemesterValidationException("Semester end date must not be before the start date.");
        }

        Name = trimmedName;
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = DateTime.UtcNow;
    }
}
