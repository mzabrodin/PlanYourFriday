using Microsoft.AspNetCore.Mvc;

using PlanYourFriday.Application.Notifications;

namespace PlanYourFriday.Api.Notifications;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(NotificationService notifications) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<NotificationView>> List(CancellationToken cancellationToken)
    {
        return notifications.ListAsync(cancellationToken);
    }

    [HttpPost("read")]
    public async Task<IActionResult> MarkRead(MarkReadRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await notifications.MarkReadAsync(request.Ids, cancellationToken);
        return NoContent();
    }
}

public sealed record MarkReadRequest(IReadOnlyList<Guid>? Ids);
