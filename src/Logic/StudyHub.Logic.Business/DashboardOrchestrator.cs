using Microsoft.Extensions.Options;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Logic.Integration.Anki;
using StudyHub.Shared.Anki;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Dashboard;

namespace StudyHub.Logic.Business;

public sealed class DashboardOrchestrator(
    ISemesterRepository semesterRepository,
    IActiveSemesterProvider activeSemesterProvider,
    ISemesterProgressCalculator semesterProgressCalculator,
    IAnkiConnectAccessor ankiConnectAccessor,
    IAnkiDueCardsCalculator ankiDueCardsCalculator,
    IOptions<AnkiConnectOptions> ankiConnectOptions
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

    public async Task<AnkiStudyStatusDto> GetAnkiStudyStatusAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (!ankiConnectOptions.Value.Enabled)
        {
            return AnkiStudyStatusDto.WithoutCounts(AnkiConnectionStatus.Disabled);
        }

        IReadOnlyList<AnkiDeckCountsDto> deckCounts;
        try
        {
            deckCounts = await ankiConnectAccessor.GetDeckCountsAsync(cancellationToken);
        }
        catch (AnkiConnectUnavailableException)
        {
            return AnkiStudyStatusDto.WithoutCounts(AnkiConnectionStatus.Unavailable);
        }

        var dueCards = ankiDueCardsCalculator.Calculate(deckCounts);

        return new AnkiStudyStatusDto(
            Status: AnkiConnectionStatus.Connected,
            HasCardsToStudy: dueCards.HasCardsToStudy,
            NewCount: dueCards.NewCount,
            LearnCount: dueCards.LearnCount,
            ReviewCount: dueCards.ReviewCount,
            Decks: dueCards.TopLevelDecks
        );
    }
}
