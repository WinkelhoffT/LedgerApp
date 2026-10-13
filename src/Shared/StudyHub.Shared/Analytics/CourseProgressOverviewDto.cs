namespace StudyHub.Shared.Analytics;

/// <summary>Progress of the active semester's courses that are not archived, ordered by name.</summary>
public sealed record CourseProgressOverviewDto(
    bool HasActiveSemester,
    string? SemesterName,
    IReadOnlyList<CourseProgressDto> Courses
)
{
    public static CourseProgressOverviewDto Empty { get; } = new(false, null, []);
}
