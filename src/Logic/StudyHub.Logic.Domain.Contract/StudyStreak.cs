namespace StudyHub.Logic.Domain.Contract;

/// <param name="Current">Consecutive study days up to today, or up to yesterday while today has no study time yet.</param>
/// <param name="Longest">The longest run of consecutive study days.</param>
public sealed record StudyStreak(int Current, int Longest);
