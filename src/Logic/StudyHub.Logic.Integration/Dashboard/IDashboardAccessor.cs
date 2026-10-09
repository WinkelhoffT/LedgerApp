using StudyHub.Shared.Calendar;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Dashboard;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Dashboard;

/// <summary>
/// Narrow HTTP access to StudyHub.Api's dashboard endpoints, covering only what the Dashboard
/// page actually calls - not a full Business-shaped management contract.
/// </summary>
public interface IDashboardAccessor
{
    Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken = default);

    Task<FlashcardsDueDto> GetFlashcardsDueAsync(CancellationToken cancellationToken = default);

    Task<CalendarDayDto> GetSessionsTodayAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UpcomingCalendarEventDto>> GetUpcomingEventsAsync(CancellationToken cancellationToken = default);
}
