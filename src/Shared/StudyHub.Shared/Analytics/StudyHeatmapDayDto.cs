namespace StudyHub.Shared.Analytics;

/// <param name="Level">0 = no study time, 1 = under 30 min, 2 = under 1 h, 3 = under 2 h, 4 = 2 h or more.</param>
public sealed record StudyHeatmapDayDto(DateOnly Date, int Minutes, int Level, bool IsFuture)
{
    public const int MaxLevel = 4;
}
