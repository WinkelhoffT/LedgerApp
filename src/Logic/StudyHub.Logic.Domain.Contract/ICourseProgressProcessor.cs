using StudyHub.Shared.Analytics;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// The progress indicators of a course. They stay separate: flashcards in Anki's buckets and practice
/// exam results are not combined into one percentage.
/// </summary>
public interface ICourseProgressProcessor
{
    /// <summary>Sums the decks' buckets; young and mature cards count as learned, in whole percent of all cards.</summary>
    FlashcardProgressDto GetFlashcardProgress(IEnumerable<FlashcardDeckProgressCounts> decks);

    /// <summary>Latest and best result in whole percent of the attempt's points.</summary>
    CourseExamResults GetExamResults(IEnumerable<PracticeExamResult> results);
}
