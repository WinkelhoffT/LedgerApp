using System.ComponentModel.DataAnnotations;

namespace StudyHub.Shared.Configuration;

/// <summary>
/// Settings for reading due-card counts from a local Anki desktop app via the AnkiConnect add-on,
/// bound from the <c>AnkiConnect</c> configuration section of StudyHub.Api. <see cref="ApiKey"/> is
/// never committed: set it via <c>AnkiConnect__ApiKey</c> if AnkiConnect is configured with one.
/// </summary>
public sealed class AnkiConnectOptions
{
    public const string SectionName = "AnkiConnect";

    public bool Enabled { get; set; }

    [Required]
    public Uri BaseAddress { get; set; } = new("http://127.0.0.1:8765/");

    public string? ApiKey { get; set; }

    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 3;
}
