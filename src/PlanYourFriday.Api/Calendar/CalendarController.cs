using Microsoft.AspNetCore.Mvc;

using PlanYourFriday.Api.Common;
using PlanYourFriday.Application.Calendar;

namespace PlanYourFriday.Api.Calendar;

[ApiController]
[Route("api/calendar")]
public sealed class CalendarController(CalendarService calendar) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CalendarItemView>> Get(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        return calendar.GetAsync(new TimeRangeRequest(from, to).ToTimeRange(), cancellationToken);
    }
}
