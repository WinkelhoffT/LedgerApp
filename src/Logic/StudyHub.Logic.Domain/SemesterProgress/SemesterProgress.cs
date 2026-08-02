namespace StudyHub.Logic.Domain.SemesterProgress;

public sealed record SemesterProgress(
    int TotalDays,
    int ElapsedDays,
    int RemainingDays,
    double PercentComplete);
