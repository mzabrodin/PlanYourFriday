using Microsoft.AspNetCore.Mvc;

using PlanYourFriday.Application.Groups;

namespace PlanYourFriday.Api.Groups;

[ApiController]
[Route("api/groups")]
public sealed class GroupsController(GroupService groups) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<GroupSummaryView>> List(CancellationToken cancellationToken)
    {
        return groups.ListAsync(cancellationToken);
    }

    [HttpGet("{groupId:guid}")]
    public Task<GroupView> Get(Guid groupId, CancellationToken cancellationToken)
    {
        return groups.GetAsync(groupId, cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<GroupView>> Create(CreateGroupRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var group = await groups.CreateAsync(
            new GroupDraft(
                request.Title.Trim(),
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                request.Window.ToTimeRange(),
                TimeSpan.FromMinutes(request.EventDurationMinutes),
                request.AvailabilityDeadline?.UtcDateTime,
                request.MemberIds ?? [],
                request.PollId),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { groupId = group.Id }, group);
    }

    [HttpPost("join/{inviteToken}")]
    public Task<JoinGroupView> Join(string inviteToken, CancellationToken cancellationToken)
    {
        return groups.JoinAsync(inviteToken, cancellationToken);
    }

    [HttpPost("{groupId:guid}/invitations")]
    public async Task<IActionResult> Invite(Guid groupId, InviteRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await groups.InviteAsync(groupId, request.Emails, cancellationToken);
        return NoContent();
    }

    [HttpPost("{groupId:guid}/members")]
    public async Task<IActionResult> AddMembers(Guid groupId, AddMembersRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await groups.AddMembersAsync(groupId, request.UserIds, cancellationToken);
        return NoContent();
    }

    [HttpDelete("/api/invitations/{invitationId:guid}")]
    public async Task<IActionResult> RevokeInvitation(Guid invitationId, CancellationToken cancellationToken)
    {
        await groups.RevokeInvitationAsync(invitationId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{groupId:guid}/availability")]
    public Task<IReadOnlyList<MemberAvailabilityView>> GetAvailability(Guid groupId, CancellationToken cancellationToken)
    {
        return groups.GetAvailabilityAsync(groupId, cancellationToken);
    }

    [HttpPut("{groupId:guid}/availability")]
    public async Task<IActionResult> UpdateAvailability(
        Guid groupId,
        UpdateAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await groups.SetAvailabilityAsync(groupId, request.Slots.Select(s => s.ToTimeRange()), cancellationToken);
        return NoContent();
    }

    [HttpGet("{groupId:guid}/optimal-slots")]
    public Task<IReadOnlyList<TimeRangeView>> GetOptimalSlots(Guid groupId, CancellationToken cancellationToken)
    {
        return groups.GetOptimalSlotsAsync(groupId, cancellationToken);
    }

    [HttpPost("{groupId:guid}/voting")]
    public async Task<IActionResult> StartVoting(
        Guid groupId,
        StartVotingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await groups.StartVotingAsync(
            groupId,
            request.Options.Select(o => o.ToTimeRange()),
            request.Deadline?.UtcDateTime,
            cancellationToken);
        return NoContent();
    }

    [HttpPut("{groupId:guid}/options/{optionId:guid}/vote")]
    public async Task<IActionResult> Vote(
        Guid groupId,
        Guid optionId,
        VoteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await groups.VoteAsync(groupId, optionId, request.Answer!.Value, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{groupId:guid}/voting")]
    public async Task<IActionResult> CancelVoting(Guid groupId, CancellationToken cancellationToken)
    {
        await groups.CancelVotingAsync(groupId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{groupId:guid}/suggestions")]
    public Task<IReadOnlyList<SuggestedTimeView>> GetSuggestions(Guid groupId, CancellationToken cancellationToken)
    {
        return groups.GetSuggestionsAsync(groupId, cancellationToken);
    }
}
