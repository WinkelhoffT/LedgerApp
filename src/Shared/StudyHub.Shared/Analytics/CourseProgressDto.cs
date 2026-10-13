namespace StudyHub.Shared.Analytics;

/// <summary>
/// Separate indicators of one course; they are not combined into one percentage.
/// </summary>
/// <param name="StudyMinutes">Study time since the semester start, within the last 365 study days.</param>
/// <param name="Flashcards">Cards of the course's decks that are not archived.</param>
/// <param name="LatestExamPercent">Result of the most recently graded practice exam attempt, in percent of its points.</param>
/// <param name="BestExamPercent">Best result of all graded practice exam attempts, in percent of their points.</param>
/// <param name="GradedAttempts">Graded attempts at the course's practice exams that are not archived.</param>
public sealed record CourseProgressDto(
    Guid CourseId,
    string Name,
    string Color,
    int StudyMinutes,
    FlashcardProgressDto Flashcards,
    int? LatestExamPercent,
    int? BestExamPercent,
    int GradedAttempts
);
