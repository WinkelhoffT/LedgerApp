using StudyHub.Shared.CalendarEvents;

namespace StudyHub.Logic.Business.Contract;

public interface ICalendarEventOrchestrator
{
    Task<CalendarEventDto> CreateAsync(
        CreateCalendarEventRequest request,
        CancellationToken cancellationToken = default
    );

    Task<CalendarEventDto> UpdateAsync(
        UpdateCalendarEventRequest request,
        CancellationToken cancellationToken = default
    );

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The next exams and deadlines from today on (at most <see cref="UpcomingCalendarEventDto.MaxCount"/>).</summary>
    Task<IReadOnlyList<UpcomingCalendarEventDto>> GetUpcomingAsync(
        CancellationToken cancellationToken = default
    );
}
