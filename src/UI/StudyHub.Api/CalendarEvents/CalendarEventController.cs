using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Contract;
using StudyHub.Shared.CalendarEvents;

namespace StudyHub.Api.CalendarEvents;

[ApiController]
[Route("api/calendar-events")]
public sealed class CalendarEventController(ICalendarEventOrchestrator eventOrchestrator)
    : ControllerBase
{
    [HttpPost]
    public Task<CalendarEventDto> CreateAsync(
        CreateCalendarEventRequest request,
        CancellationToken cancellationToken
    ) => eventOrchestrator.CreateAsync(request, cancellationToken);

    // The route id always wins over whatever Id is present in the request body.
    [HttpPut("{id:guid}")]
    public Task<CalendarEventDto> UpdateAsync(
        Guid id,
        UpdateCalendarEventRequest request,
        CancellationToken cancellationToken
    ) => eventOrchestrator.UpdateAsync(request with { Id = id }, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<NoContentResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await eventOrchestrator.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
