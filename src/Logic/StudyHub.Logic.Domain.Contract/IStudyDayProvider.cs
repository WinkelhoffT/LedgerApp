namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Study-day boundaries. Like Anki's "next day starts at 4", a study day starts at a configured hour
/// in a configured time zone (<c>FlashcardStudyOptions</c>), not at midnight UTC.
/// </summary>
public interface IStudyDayProvider
{
    /// <summary>The current study day, read from the clock.</summary>
    StudyDay GetCurrent();

    /// <summary>The study day a UTC instant belongs to.</summary>
    DateOnly GetDate(DateTime utc);

    /// <summary>The UTC instant at which the study day <paramref name="date"/> begins.</summary>
    DateTime GetStart(DateOnly date);
}
