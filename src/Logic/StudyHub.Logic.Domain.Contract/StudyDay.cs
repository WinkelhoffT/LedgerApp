namespace StudyHub.Logic.Domain.Contract;

/// <summary>The study day around <see cref="Now"/>; all instants are UTC.</summary>
/// <param name="Start">When <see cref="Date"/> began.</param>
/// <param name="NextStart">When the next study day begins; a card due before it is due today.</param>
public sealed record StudyDay(DateTime Now, DateOnly Date, DateTime Start, DateTime NextStart);
