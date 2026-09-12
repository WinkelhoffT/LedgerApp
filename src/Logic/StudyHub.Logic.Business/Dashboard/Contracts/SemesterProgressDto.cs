namespace StudyHub.Logic.Business.Dashboard;

public sealed record SemesterProgressDto(
    bool HasActiveSemester,
    Guid? SemesterId,
    string? SemesterName,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? TotalDays,
    int? ElapsedDays,
    int? RemainingDays,
    double? PercentComplete)
{
    public static SemesterProgressDto Empty { get; } = new(
        HasActiveSemester: false,
        SemesterId: null,
        SemesterName: null,
        StartDate: null,
        EndDate: null,
        TotalDays: null,
        ElapsedDays: null,
        RemainingDays: null,
        PercentComplete: null);
}
