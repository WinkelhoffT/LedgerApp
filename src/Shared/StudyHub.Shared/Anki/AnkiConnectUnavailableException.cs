namespace StudyHub.Shared.Anki;

/// <summary>
/// AnkiConnect could not be reached (Anki not running, add-on missing, timeout) or answered with an
/// error. Anki not running is a normal situation, so callers translate this into a status, not a 500.
/// </summary>
public sealed class AnkiConnectUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
