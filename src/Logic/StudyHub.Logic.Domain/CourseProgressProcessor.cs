using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Analytics;

namespace StudyHub.Logic.Domain;

public sealed class CourseProgressProcessor : ICourseProgressProcessor
{
    public FlashcardProgressDto GetFlashcardProgress(IEnumerable<FlashcardDeckProgressCounts> decks)
    {
        var deckList = decks.ToList();
        var newCards = deckList.Sum(d => d.New);
        var learning = deckList.Sum(d => d.Learning);
        var young = deckList.Sum(d => d.Young);
        var mature = deckList.Sum(d => d.Mature);
        var total = newCards + learning + young + mature;

        return new FlashcardProgressDto(
            newCards,
            learning,
            young,
            mature,
            total,
            total == 0 ? null : ToPercent(young + mature, total)
        );
    }

    public CourseExamResults GetExamResults(IEnumerable<PracticeExamResult> results)
    {
        var graded = results.OrderBy(r => r.GradedAt).ToList();
        if (graded.Count == 0)
        {
            return new CourseExamResults(null, null, 0);
        }

        return new CourseExamResults(ToPercent(graded[^1]), graded.Max(ToPercent), graded.Count);
    }

    private static int ToPercent(PracticeExamResult result) =>
        result.MaxPoints == 0 ? 0 : ToPercent(result.AwardedPoints, result.MaxPoints);

    private static int ToPercent(int part, int whole) =>
        (int)Math.Round(part * 100.0 / whole, MidpointRounding.AwayFromZero);
}
