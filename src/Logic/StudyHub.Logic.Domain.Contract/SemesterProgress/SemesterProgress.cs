namespace StudyHub.Logic.Domain.Contract.SemesterProgress;

public sealed record SemesterProgress(
    int TotalDays,
    int ElapsedDays,
    int RemainingDays,
    double PercentComplete);
