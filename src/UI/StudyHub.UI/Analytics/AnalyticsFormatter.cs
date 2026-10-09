using System.Globalization;
using StudyHub.Shared.Analytics;

namespace StudyHub.UI.Analytics;

/// <summary>
/// Display texts and chart geometry for the analytics charts. The Api computes every value; this
/// only formats them and scales them to the chart size.
/// </summary>
public static class AnalyticsFormatter
{
    private const int MinutesPerHour = 60;

    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <summary><c>6 h 30 min</c>, <c>2 h</c> or <c>45 min</c>.</summary>
    public static string FormatDuration(int minutes)
    {
        var hours = minutes / MinutesPerHour;
        var rest = minutes % MinutesPerHour;

        return (hours, rest) switch
        {
            (0, _) => $"{rest} min",
            (_, 0) => $"{hours} h",
            _ => $"{hours} h {rest} min",
        };
    }

    /// <summary><c>24.5 h</c>, rounded to a tenth of an hour.</summary>
    public static string FormatHours(int minutes) =>
        $"{(minutes / (double)MinutesPerHour).ToString("0.#", Culture)} h";

    /// <summary><c>+1.5 h vs. last week</c>, <c>−45 min vs. last week</c> or <c>Same as last week</c>.</summary>
    public static string FormatHoursDifference(int thisWeekMinutes, int lastWeekMinutes) =>
        FormatDifference(
            thisWeekMinutes - lastWeekMinutes,
            minutes =>
                Math.Abs(minutes) < MinutesPerHour
                    ? FormatDuration(Math.Abs(minutes))
                    : FormatHours(Math.Abs(minutes))
        );

    /// <summary><c>+58 vs. last week</c>, <c>−12 vs. last week</c> or <c>Same as last week</c>.</summary>
    public static string FormatCountDifference(int thisWeek, int lastWeek) =>
        FormatDifference(thisWeek - lastWeek, count => Math.Abs(count).ToString(Culture));

    /// <summary><c>up</c>, <c>down</c> or <c>same</c>, for the color of a difference.</summary>
    public static string GetTrendClass(int thisWeek, int lastWeek) =>
        thisWeek.CompareTo(lastWeek) switch
        {
            > 0 => "up",
            < 0 => "down",
            _ => "same",
        };

    /// <summary><c>1 day</c> or <c>18 days</c>.</summary>
    public static string FormatDays(int days) => days == 1 ? "1 day" : $"{days} days";

    /// <summary>The line under the streak: <c>18 days in a row — your best is 21.</c></summary>
    public static string FormatStreak(int currentDays, int longestDays) =>
        (currentDays, longestDays) switch
        {
            (0, 0) => "No streak yet. Study today to start one.",
            (0, _) => $"No current streak. Your best is {FormatDays(longestDays)}.",
            _ when currentDays == longestDays =>
                $"{FormatDays(currentDays)} in a row — your best so far. Keep it going!",
            _ => $"{FormatDays(currentDays)} in a row — your best is {FormatDays(longestDays)}.",
        };

    /// <summary><c>Practice exams: latest 50 %, best 90 % (3 graded)</c>, or a hint without graded attempts.</summary>
    public static string FormatExamResults(CourseProgressDto course) =>
        course.GradedAttempts == 0
            ? "No graded practice exam yet"
            : $"Practice exams: latest {FormatPercent(course.LatestExamPercent)}, best {FormatPercent(course.BestExamPercent)} ({course.GradedAttempts} graded)";

    /// <summary><c>72 % learned</c>, or <c>No flashcards yet</c>.</summary>
    public static string FormatLearned(FlashcardProgressDto progress) =>
        progress.LearnedPercent is { } percent ? $"{percent} % learned" : "No flashcards yet";

    /// <summary><c>72 %</c>, or <c>—</c> without a value.</summary>
    public static string FormatPercent(int? percent) => percent is { } value ? $"{value} %" : "—";

    /// <summary><c>Mon</c>.</summary>
    public static string FormatShortWeekday(DateOnly date) => date.ToString("ddd", Culture);

    /// <summary><c>M</c> for Monday.</summary>
    public static string FormatWeekdayLetter(DateOnly date) => date.ToString("ddd", Culture)[..1];

    /// <summary><c>Thu 8 Oct</c>.</summary>
    public static string FormatShortDate(DateOnly date) => date.ToString("ddd d MMM", Culture);

    /// <summary><c>W41</c>, or <c>Now</c> for the current week.</summary>
    public static string FormatWeekLabel(StudyWeekDto week, bool isCurrent) =>
        isCurrent ? "Now" : $"W{week.IsoWeek}";

    /// <summary>Tooltip of a day bar: <c>Thu 8 Oct: 2 h 10 min, 120 flashcards</c>.</summary>
    public static string FormatDayTitle(StudyDayDto day) =>
        day.ReviewCount > 0
            ? $"{FormatShortDate(day.Date)}: {FormatDuration(day.Minutes)}, {FormatFlashcards(day.ReviewCount)}"
            : $"{FormatShortDate(day.Date)}: {FormatDuration(day.Minutes)}";

    /// <summary>Tooltip of a heatmap cell: <c>Thu 8 Oct: 2 h 10 min</c>.</summary>
    public static string FormatHeatmapTitle(StudyHeatmapDayDto day) =>
        $"{FormatShortDate(day.Date)}: {FormatDuration(day.Minutes)}";

    /// <summary>Tooltip of a week point: <c>Week 41 (5 Oct): 12 h 30 min</c>.</summary>
    public static string FormatWeekTitle(StudyWeekDto week) =>
        $"Week {week.IsoWeek} ({week.WeekStart.ToString("d MMM", Culture)}): {FormatDuration(week.Minutes)}";

    /// <summary><c>1 flashcard</c> or <c>120 flashcards</c>.</summary>
    public static string FormatFlashcards(int count) =>
        count == 1 ? "1 flashcard" : $"{count} flashcards";

    /// <summary>Screen-reader summary of the week chart: <c>Study time this week: 6 h 30 min, most on Thursday with 2 h 10 min.</c></summary>
    public static string FormatWeekSummary(IReadOnlyList<StudyDayDto> days)
    {
        var total = days.Sum(d => d.Minutes);
        if (total == 0)
        {
            return "Study time this week: none yet.";
        }

        var best = days.MaxBy(d => d.Minutes)!;
        return $"Study time this week: {FormatDuration(total)}, most on {best.Date.ToString("dddd", Culture)} with {FormatDuration(best.Minutes)}.";
    }

    /// <summary>Screen-reader summary of the weekly chart: <c>Study time per week, last 8 weeks: from 3 h to 12 h 30 min; this week 6 h.</c></summary>
    public static string FormatWeeksSummary(IReadOnlyList<StudyWeekDto> weeks) =>
        weeks.Count == 0
            ? "Study time per week: no data."
            : $"Study time per week, last {weeks.Count} weeks: from {FormatDuration(weeks.Min(w => w.Minutes))} to {FormatDuration(weeks.Max(w => w.Minutes))}; this week {FormatDuration(weeks[^1].Minutes)}.";

    /// <summary>Screen-reader summary of the heatmap: <c>Study heatmap, last 5 weeks: studied on 18 of 31 days, 42 h 10 min in total.</c></summary>
    public static string FormatHeatmapSummary(IReadOnlyList<StudyHeatmapDayDto> days)
    {
        var past = days.Where(d => !d.IsFuture).ToList();
        return $"Study heatmap, last {days.Count / 7} weeks: studied on {past.Count(d => d.Minutes > 0)} of {past.Count} days, {FormatDuration(past.Sum(d => d.Minutes))} in total.";
    }

    /// <summary>Screen-reader summary of the streak strip: <c>This week you studied on Monday, Tuesday and Wednesday.</c></summary>
    public static string FormatStreakWeekSummary(IReadOnlyList<StudyDayDto> days)
    {
        var studied = days.Where(d => d.Minutes > 0)
            .Select(d => d.Date.ToString("dddd", Culture))
            .ToList();

        return studied.Count switch
        {
            0 => "This week you have not studied yet.",
            1 => $"This week you studied on {studied[0]}.",
            _ => $"This week you studied on {string.Join(", ", studied[..^1])} and {studied[^1]}.",
        };
    }

    /// <summary>Screen-reader label of a course's flashcard bar: <c>72 % learned: 40 mature, 32 young, 10 learning, 18 new.</c></summary>
    public static string FormatFlashcardProgress(FlashcardProgressDto progress) =>
        progress.Total == 0
            ? "No flashcards yet."
            : $"{FormatPercent(progress.LearnedPercent)} learned: {progress.Mature} mature, {progress.Young} young, {progress.Learning} learning, {progress.New} new.";

    /// <summary>Height of a bar in percent of the tallest one, as a CSS value.</summary>
    public static string GetBarHeight(int minutes, int maxMinutes) =>
        ToCss(maxMinutes == 0 ? 0 : minutes * 100.0 / maxMinutes);

    /// <summary>Width of a part in percent of the whole, as a CSS value.</summary>
    public static string GetShare(int part, int whole) =>
        ToCss(whole == 0 ? 0 : part * 100.0 / whole);

    /// <summary>
    /// The points of a line chart in a <paramref name="width"/> × <paramref name="height"/> box. Each
    /// value sits in the middle of an equal column, so labels in a grid below line up with it; the
    /// highest value touches the top padding.
    /// </summary>
    public static IReadOnlyList<ChartPoint> GetLinePoints(
        IReadOnlyList<int> values,
        double width,
        double height,
        double verticalPadding
    )
    {
        var max = Math.Max(values.DefaultIfEmpty(0).Max(), 1);
        var column = values.Count == 0 ? 0 : width / values.Count;

        return values
            .Select(
                (value, index) =>
                    new ChartPoint(
                        (index + 0.5) * column,
                        height
                            - verticalPadding
                            - value / (double)max * (height - verticalPadding * 2)
                    )
            )
            .ToList();
    }

    /// <summary>The SVG path through <paramref name="points"/>.</summary>
    public static string GetLinePath(IReadOnlyList<ChartPoint> points) =>
        string.Join(
            ' ',
            points.Select((p, i) => $"{(i == 0 ? 'M' : 'L')}{ToSvg(p.X)},{ToSvg(p.Y)}")
        );

    /// <summary>The SVG path of the area between the line and the bottom of the chart.</summary>
    public static string GetAreaPath(IReadOnlyList<ChartPoint> points, double bottom) =>
        points.Count == 0
            ? string.Empty
            : $"{GetLinePath(points)} L{ToSvg(points[^1].X)},{ToSvg(bottom)} L{ToSvg(points[0].X)},{ToSvg(bottom)} Z";

    /// <summary>A coordinate for an SVG attribute.</summary>
    public static string ToSvg(double value) => value.ToString("0.#", Culture);

    private static string ToCss(double percent) => $"{percent.ToString("0.##", Culture)}%";

    private static string FormatDifference(int difference, Func<int, string> format) =>
        difference switch
        {
            > 0 => $"+{format(difference)} vs. last week",
            < 0 => $"−{format(difference)} vs. last week",
            _ => "Same as last week",
        };
}
