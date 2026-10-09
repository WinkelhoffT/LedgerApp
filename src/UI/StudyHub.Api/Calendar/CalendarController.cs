using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using StudyHub.Logic.Business.Contract;
using StudyHub.Shared.Calendar;

namespace StudyHub.Api.Calendar;

[ApiController]
[Route("api/calendar")]
public sealed class CalendarController(ICalendarOrchestrator calendarOrchestrator) : ControllerBase
{
    [HttpGet("month")]
    public Task<CalendarMonthDto> GetMonthAsync(
        [FromQuery, BindRequired, Range(CalendarMonthDto.MinYear, CalendarMonthDto.MaxYear)]
            int year,
        [FromQuery, BindRequired, Range(1, 12)] int month,
        CancellationToken cancellationToken
    ) => calendarOrchestrator.GetMonthAsync(year, month, cancellationToken);

    // The upper bound keeps the week's Sunday inside the range DateOnly can represent.
    [HttpGet("week")]
    public Task<CalendarWeekDto> GetWeekAsync(
        [
            FromQuery,
            BindRequired,
            Range(
                typeof(DateOnly),
                "0001-01-01",
                "9998-12-31",
                ParseLimitsInInvariantCulture = true,
                ConvertValueInInvariantCulture = true
            )
        ]
            DateOnly date,
        CancellationToken cancellationToken
    ) => calendarOrchestrator.GetWeekAsync(date, cancellationToken);
}
