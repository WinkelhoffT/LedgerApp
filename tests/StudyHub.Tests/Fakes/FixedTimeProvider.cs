namespace StudyHub.Tests.Fakes;

/// <summary>A clock that stands still at <see cref="UtcNow"/> until a test moves it.</summary>
public sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
{
    public DateTime UtcNow { get; set; } = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

    public override DateTimeOffset GetUtcNow() => new(UtcNow);
}
