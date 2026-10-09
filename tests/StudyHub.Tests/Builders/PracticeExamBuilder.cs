using StudyHub.Shared.PracticeExams;

namespace StudyHub.Tests.Builders;

/// <summary>Practice exams, tasks and attempts for tests.</summary>
internal static class PracticeExamBuilder
{
    public static readonly DateTime CreatedAt = new(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc);

    public static GeneratedPracticeExamTask SingleChoice(
        string text = "Welche Laufzeit hat Dijkstra mit Binärheap?",
        int points = 2,
        int? sourceId = 1,
        int correctIndex = 0,
        int optionCount = 4
    ) =>
        new(
            PracticeExamTaskKind.SingleChoice,
            text,
            points,
            Enumerable
                .Range(0, optionCount)
                .Select(i => new GeneratedPracticeExamOption(
                    $"Option {i + 1}",
                    i == correctIndex,
                    $"Begründung {i + 1}"
                ))
                .ToList(),
            [],
            "Erklärung des Konzepts.",
            sourceId
        );

    /// <param name="criterionPoints">Points per rubric criterion; 2 and 3 when <c>null</c>.</param>
    public static GeneratedPracticeExamTask Open(
        int[]? criterionPoints = null,
        string text = "Erklären Sie, warum Dijkstra keine negativen Kanten erlaubt.",
        int? sourceId = 1
    ) =>
        new(
            PracticeExamTaskKind.Open,
            text,
            (criterionPoints ?? [2, 3]).Sum(),
            [],
            (criterionPoints ?? [2, 3])
                .Select(
                    (points, i) => new GeneratedPracticeExamCriterion($"Kriterium {i + 1}", points)
                )
                .ToList(),
            "Musterlösung.",
            sourceId
        );

    /// <summary>
    /// An exam with a single-choice task (2 points, the second option correct), an open task
    /// (criteria worth 2 and 3 points) and a second open task (4 points).
    /// </summary>
    public static StoredPracticeExam Exam(
        bool isArchived = false,
        int durationMinutes = 60,
        bool excludeLastTask = false
    )
    {
        var exam = new PracticeExam(
            Guid.NewGuid(),
            "Probeklausur Algorithmen · Universität · 09.10.2026",
            PracticeExamLevel.University,
            durationMinutes,
            PracticeExamSourceKind.Course,
            Guid.NewGuid(),
            null,
            "claude-sonnet-5-5",
            "practice-exam-v1",
            null,
            isArchived,
            CreatedAt,
            CreatedAt
        );

        return new StoredPracticeExam(
            exam,
            [
                SingleChoiceTask(exam.Id, 1),
                OpenTask(exam.Id, 2, [2, 3]),
                OpenTask(exam.Id, 3, [4], excludeLastTask),
            ]
        );
    }

    public static StoredPracticeExamTask SingleChoiceTask(Guid examId, int position)
    {
        var task = new PracticeExamTask(
            Guid.NewGuid(),
            examId,
            position,
            PracticeExamTaskKind.SingleChoice,
            $"Aufgabe {position}",
            2,
            "Erklärung",
            null,
            null,
            false,
            CreatedAt
        );

        return new StoredPracticeExamTask(
            task,
            Enumerable
                .Range(1, 4)
                .Select(i => new PracticeExamOption(
                    Guid.NewGuid(),
                    task.Id,
                    i,
                    $"Option {i}",
                    $"Begründung {i}",
                    i == 2
                ))
                .ToList(),
            []
        );
    }

    public static StoredPracticeExamTask OpenTask(
        Guid examId,
        int position,
        int[] criterionPoints,
        bool isExcluded = false
    )
    {
        var task = new PracticeExamTask(
            Guid.NewGuid(),
            examId,
            position,
            PracticeExamTaskKind.Open,
            $"Aufgabe {position}",
            criterionPoints.Sum(),
            "Musterlösung",
            null,
            null,
            isExcluded,
            CreatedAt
        );

        return new StoredPracticeExamTask(
            task,
            [],
            criterionPoints
                .Select(
                    (points, i) =>
                        new PracticeExamCriterion(
                            Guid.NewGuid(),
                            task.Id,
                            i + 1,
                            $"Kriterium {i + 1}",
                            points
                        )
                )
                .ToList()
        );
    }

    public static PracticeExamAttempt Attempt(
        int maxPoints = 10,
        int? awardedPoints = null,
        DateTime? startedAt = null,
        DateTime? submittedAt = null,
        DateTime? gradedAt = null
    ) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            startedAt ?? CreatedAt,
            null,
            submittedAt,
            gradedAt,
            maxPoints,
            awardedPoints
        );
}
