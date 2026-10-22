using Microsoft.AspNetCore.Mvc;

using PlanYourFriday.Application.Friends;

namespace PlanYourFriday.Api.Friends;

[ApiController]
[Route("api")]
public sealed class FriendsController(FriendService friends) : ControllerBase
{
    [HttpGet("friends")]
    public Task<FriendsOverviewView> GetOverview(CancellationToken cancellationToken)
    {
        return friends.GetOverviewAsync(cancellationToken);
    }

    [HttpPost("friends/by-code/{code}")]
    public Task<PersonView> AddByCode(string code, CancellationToken cancellationToken)
    {
        return friends.AddByFriendCodeAsync(code, cancellationToken);
    }

    [HttpPost("friends/requests")]
    public Task<PersonView> SendRequest(SendFriendRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return friends.SendRequestAsync(request.UserId!.Value, cancellationToken);
    }

    [HttpPost("friends/requests/{requestId:guid}/accept")]
    public async Task<IActionResult> AcceptRequest(Guid requestId, CancellationToken cancellationToken)
    {
        await friends.AcceptAsync(requestId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("friends/requests/{requestId:guid}")]
    public async Task<IActionResult> RemoveRequest(Guid requestId, CancellationToken cancellationToken)
    {
        await friends.RemoveRequestAsync(requestId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("friends/{userId:guid}")]
    public async Task<IActionResult> RemoveFriend(Guid userId, CancellationToken cancellationToken)
    {
        await friends.RemoveFriendAsync(userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("people")]
    public Task<IReadOnlyList<PersonView>> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        return friends.SearchAsync(q, cancellationToken);
    }

    [HttpGet("people/by-code/{code}")]
    public Task<PersonView> GetByCode(string code, CancellationToken cancellationToken)
    {
        return friends.GetByFriendCodeAsync(code, cancellationToken);
    }
}
