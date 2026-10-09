using StudyHub.Shared.PracticeExams;

namespace StudyHub.UI.PracticeExams;

/// <summary>Display texts for practice exams. The level names are the German school types.</summary>
public static class PracticeExamFormatter
{
    public static IReadOnlyList<PracticeExamLevel> Levels { get; } =
    [PracticeExamLevel.SecondarySchool, PracticeExamLevel.Gymnasium, PracticeExamLevel.University];

    public static string FormatLevel(PracticeExamLevel level) =>
        level switch
        {
            PracticeExamLevel.SecondarySchool => "Oberschule",
            PracticeExamLevel.Gymnasium => "Gymnasium",
            _ => "Universität",
        };

    public static string DescribeLevel(PracticeExamLevel level) =>
        level switch
        {
            PracticeExamLevel.SecondarySchool => "Basics and simple application",
            PracticeExamLevel.Gymnasium => "Abitur level: explain, justify, transfer",
            _ => "Transfer, design, proofs",
        };

    public static string FormatPoints(int points) => points == 1 ? "1 point" : $"{points} points";

    /// <summary>"41 / 60 (68 %)", or "—" while the attempt is not graded.</summary>
    public static string FormatResult(PracticeExamAttemptSummaryDto? attempt) =>
        attempt is { AwardedPoints: { } awarded, Percent: { } percent }
            ? $"{awarded} / {attempt.MaxPoints} ({percent} %)"
            : "—";

    public static string FormatStatus(PracticeExamAttemptStatus status) =>
        status switch
        {
            PracticeExamAttemptStatus.InProgress => "In progress",
            PracticeExamAttemptStatus.Submitted => "Self-grading open",
            _ => "Graded",
        };

    public static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.Zero)
        {
            return "0:00";
        }

        return remaining.TotalHours >= 1
            ? $"{(int)remaining.TotalHours}:{remaining.Minutes:00}:{remaining.Seconds:00}"
            : $"{remaining.Minutes}:{remaining.Seconds:00}";
    }
}
