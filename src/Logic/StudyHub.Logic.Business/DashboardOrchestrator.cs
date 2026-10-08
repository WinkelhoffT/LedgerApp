using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Dashboard;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Business;

public sealed class DashboardOrchestrator(
    ISemesterRepository semesterRepository,
    IActiveSemesterProvider activeSemesterProvider,
    ISemesterProgressCalculator semesterProgressCalculator,
    IFlashcardDeckRepository deckRepository,
    IFlashcardRepository flashcardRepository,
    IStudyQueueProvider studyQueueProvider,
    IStudyDayProvider studyDayProvider
) : IDashboardOrchestrator
{
    public async Task<SemesterProgressDto> GetSemesterProgressAsync(
        CancellationToken cancellationToken = default
    )
    {
        var semesters = await semesterRepository.GetAllAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var activeSemester = activeSemesterProvider.GetActive(semesters, today);
        if (activeSemester is null)
        {
            return SemesterProgressDto.Empty;
        }

        var progress = semesterProgressCalculator.Calculate(
            activeSemester.StartDate,
            activeSemester.EndDate,
            today
        );

        return new SemesterProgressDto(
            HasActiveSemester: true,
            SemesterId: activeSemester.Id,
            SemesterName: activeSemester.Name,
            StartDate: activeSemester.StartDate,
            EndDate: activeSemester.EndDate,
            TotalDays: progress.TotalDays,
            ElapsedDays: progress.ElapsedDays,
            RemainingDays: progress.RemainingDays,
            PercentComplete: progress.PercentComplete
        );
    }

    public async Task<FlashcardsDueDto> GetFlashcardsDueAsync(CancellationToken cancellationToken = default)
    {
        var decks = (await deckRepository.GetAllAsync(cancellationToken)).Where(d => !d.IsArchived).ToList();
        var studyDay = studyDayProvider.GetCurrent();
        var cardCounts = (await flashcardRepository.GetCardCountsAsync(studyDay.NextStart, cancellationToken: cancellationToken))
            .ToDictionary(c => c.DeckId);
        var reviewCounts = (await flashcardRepository.GetReviewCountsAsync(studyDay.Start, cancellationToken: cancellationToken))
            .ToDictionary(c => c.DeckId);

        var deckCounts = decks
            .Select(deck => new FlashcardDeckDueDto(
                deck.Id,
                deck.Name,
                studyQueueProvider.GetCounts(deck, cardCounts.GetValueOrDefault(deck.Id), reviewCounts.GetValueOrDefault(deck.Id))))
            .ToList();

        var total = new FlashcardStudyCountsDto(
            deckCounts.Sum(d => d.Counts.New),
            deckCounts.Sum(d => d.Counts.Learning),
            deckCounts.Sum(d => d.Counts.Review));

        var topDecks = deckCounts
            .Where(d => d.Counts.Total > 0)
            .OrderByDescending(d => d.Counts.Total)
            .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .Take(FlashcardsDueDto.MaxDecks)
            .ToList();

        return new FlashcardsDueDto(total, topDecks);
    }
}
