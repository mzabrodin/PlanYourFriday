using Microsoft.AspNetCore.Mvc;

using PlanYourFriday.Application.Polls;

namespace PlanYourFriday.Api.Polls;

[ApiController]
[Route("api/polls")]
public sealed class PollsController(PollService polls) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PollSummaryView>> List(CancellationToken cancellationToken)
    {
        return polls.ListAsync(cancellationToken);
    }

    [HttpGet("{pollId:guid}")]
    public Task<PollView> Get(Guid pollId, CancellationToken cancellationToken)
    {
        return polls.GetAsync(pollId, cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<PollView>> Create(CreatePollRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var poll = await polls.CreateAsync(request.ToDraft(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { pollId = poll.Id }, poll);
    }

    [HttpPost("{pollId:guid}/options")]
    public Task<PollView> AddOption(Guid pollId, AddPollOptionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return polls.AddOptionAsync(pollId, request.Title, cancellationToken);
    }

    [HttpPut("{pollId:guid}/votes")]
    public Task<PollView> Vote(Guid pollId, VotePollRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return polls.VoteAsync(pollId, request.OptionIds, cancellationToken);
    }

    [HttpPost("{pollId:guid}/close")]
    public Task<PollView> Close(Guid pollId, CancellationToken cancellationToken)
    {
        return polls.CloseAsync(pollId, cancellationToken);
    }

    [HttpDelete("{pollId:guid}")]
    public async Task<IActionResult> Delete(Guid pollId, CancellationToken cancellationToken)
    {
        await polls.DeleteAsync(pollId, cancellationToken);
        return NoContent();
    }
}
