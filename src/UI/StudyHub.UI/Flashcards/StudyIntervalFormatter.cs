using System.Globalization;

namespace StudyHub.UI.Flashcards;

/// <summary>Formats the interval behind an answer button the way Anki does: <c>&lt;1m</c>, <c>&lt;10m</c>, <c>4d</c>, <c>1.5mo</c>, <c>2y</c>.</summary>
public static class StudyIntervalFormatter
{
    public static string Format(TimeSpan interval)
    {
        if (interval < TimeSpan.FromHours(1))
        {
            return $"<{Math.Ceiling(interval.TotalMinutes).ToString(CultureInfo.InvariantCulture)}m";
        }

        if (interval < TimeSpan.FromDays(1))
        {
            return $"<{Math.Ceiling(interval.TotalHours).ToString(CultureInfo.InvariantCulture)}h";
        }

        var days = interval.TotalDays;
        return days switch
        {
            < 30 => $"{days.ToString("0", CultureInfo.InvariantCulture)}d",
            < 365 => $"{(days / 30).ToString("0.#", CultureInfo.InvariantCulture)}mo",
            _ => $"{(days / 365).ToString("0.#", CultureInfo.InvariantCulture)}y",
        };
    }
}
