using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract;
using StudyHub.Shared.Calendar;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Dashboard;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Api.Dashboard;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(
    IDashboardOrchestrator dashboardOrchestrator,
    ICalendarOrchestrator calendarOrchestrator,
    ICalendarEventOrchestrator eventOrchestrator
) : ControllerBase
{
    [HttpGet("semester-progress")]
    public Task<SemesterProgressDto> GetSemesterProgressAsync(
        CancellationToken cancellationToken
    ) => dashboardOrchestrator.GetSemesterProgressAsync(cancellationToken);

    [HttpGet("flashcards-due")]
    public Task<FlashcardsDueDto> GetFlashcardsDueAsync(CancellationToken cancellationToken) =>
        dashboardOrchestrator.GetFlashcardsDueAsync(cancellationToken);

    [HttpGet("sessions-today")]
    public Task<CalendarDayDto> GetSessionsTodayAsync(CancellationToken cancellationToken) =>
        calendarOrchestrator.GetTodayAsync(cancellationToken);

    [HttpGet("upcoming-events")]
    public Task<IReadOnlyList<UpcomingCalendarEventDto>> GetUpcomingEventsAsync(CancellationToken cancellationToken) =>
        eventOrchestrator.GetUpcomingAsync(cancellationToken);
}
