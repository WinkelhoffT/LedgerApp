namespace StudyHub.Shared.Configuration;

/// <summary>
/// When a study day starts, bound from the <c>Flashcards</c> configuration section of StudyHub.Api.
/// As in Anki ("next day starts at 4"), a study day runs from <see cref="DayStartHour"/> to the same
/// hour of the next day in <see cref="TimeZone"/>.
/// </summary>
public sealed class FlashcardStudyOptions
{
    public const string SectionName = "Flashcards";

    /// <summary>IANA time zone id.</summary>
    public string TimeZone { get; set; } = "Europe/Berlin";

    /// <summary>Hour of the day (0-23) at which a new study day begins.</summary>
    public int DayStartHour { get; set; } = 4;
}
