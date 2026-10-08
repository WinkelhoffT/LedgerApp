namespace StudyHub.Shared.Configuration;

/// <summary>
/// Calendar settings, bound from the <c>Calendar</c> configuration section of StudyHub.Api. Sessions
/// are stored as local wall-clock times, so the time zone only decides which date is "today".
/// </summary>
public sealed class CalendarOptions
{
    public const string SectionName = "Calendar";

    /// <summary>IANA time zone id.</summary>
    public string TimeZone { get; set; } = "Europe/Berlin";
}
