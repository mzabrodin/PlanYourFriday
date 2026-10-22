using Microsoft.AspNetCore.Mvc;

using PlanYourFriday.Application.Events;

namespace PlanYourFriday.Api.Events;

[ApiController]
[Route("api")]
public sealed class EventsController(EventService events) : ControllerBase
{
    [HttpPost("groups/{groupId:guid}/event")]
    public async Task<EventView> Confirm(Guid groupId, ConfirmEventRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await events.ConfirmAsync(groupId, request.ToDetails(), request.Time.ToTimeRange(), cancellationToken);
    }

    [HttpPatch("events/{eventId:guid}")]
    public async Task<EventView> Update(Guid eventId, EventDetailsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await events.UpdateAsync(eventId, request.ToDetails(), cancellationToken);
    }

    [HttpDelete("events/{eventId:guid}")]
    public async Task<IActionResult> Cancel(Guid eventId, CancellationToken cancellationToken)
    {
        await events.CancelAsync(eventId, cancellationToken);
        return NoContent();
    }
}
