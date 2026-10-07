namespace StudyHub.Shared.Semesters;

public sealed record SemesterProgress(
    int TotalDays,
    int ElapsedDays,
    int RemainingDays,
    double PercentComplete
);
