using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Friends;
using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Polls;

namespace PlanYourFriday.Application.Polls;

public sealed class PollService(
    ICurrentUser currentUser,
    IPollRepository polls,
    IPollQueries queries,
    IFriendshipRepository friendships,
    NotificationPublisher publisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task<IReadOnlyList<PollSummaryView>> ListAsync(CancellationToken cancellationToken)
    {
        return queries.ListAsync(currentUser.UserId, Now(), cancellationToken);
    }

    public async Task<PollView> GetAsync(Guid pollId, CancellationToken cancellationToken)
    {
        return await queries.GetAsync(pollId, currentUser.UserId, Now(), cancellationToken)
            ?? throw new EntityNotFoundException("poll.not_found", "Poll not found.");
    }

    public async Task<PollView> CreateAsync(PollDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        await friendships.EnsureFriendsAsync(currentUser.UserId, draft.ParticipantIds, cancellationToken);
        var poll = Poll.Create(
            currentUser.UserId,
            draft.Title,
            draft.Description,
            draft.ParticipantIds,
            draft.Options,
            draft.Deadline,
            draft.MultipleChoice,
            draft.AllowNewOptions,
            Now());
        polls.Add(poll);
        publisher.NotifyParticipants(poll, NotificationKind.PollStarted, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetAsync(poll.Id, cancellationToken);
    }

    public async Task<PollView> AddOptionAsync(Guid pollId, string title, CancellationToken cancellationToken)
    {
        var poll = (await polls.GetByIdAsync(pollId, cancellationToken)).EnsureParticipant(currentUser.UserId);
        poll.AddOption(currentUser.UserId, title, Now());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetAsync(poll.Id, cancellationToken);
    }

    public async Task<PollView> VoteAsync(Guid pollId, IReadOnlyCollection<Guid> optionIds, CancellationToken cancellationToken)
    {
        var poll = (await polls.GetByIdAsync(pollId, cancellationToken)).EnsureParticipant(currentUser.UserId);
        poll.Vote(currentUser.UserId, optionIds, Now());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetAsync(poll.Id, cancellationToken);
    }

    public async Task<PollView> CloseAsync(Guid pollId, CancellationToken cancellationToken)
    {
        var poll = (await polls.GetByIdAsync(pollId, cancellationToken)).EnsureParticipant(currentUser.UserId);
        var wasClosed = poll.IsClosed(Now());
        poll.Close(currentUser.UserId, Now());
        if (!wasClosed)
        {
            Announce(poll, currentUser.UserId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetAsync(poll.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid pollId, CancellationToken cancellationToken)
    {
        var poll = (await polls.GetByIdAsync(pollId, cancellationToken)).EnsureParticipant(currentUser.UserId);
        poll.EnsureCreator(currentUser.UserId);
        polls.Remove(poll);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> AnnounceClosedAsync(CancellationToken cancellationToken)
    {
        var closed = await polls.ListClosedUnannouncedAsync(Now(), cancellationToken);
        foreach (var poll in closed)
        {
            Announce(poll, null);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return closed.Count;
    }

    private void Announce(Poll poll, Guid? actorId)
    {
        poll.MarkClosedNotified();
        publisher.NotifyParticipants(poll, NotificationKind.PollClosed, actorId);
    }

    private DateTime Now()
    {
        return timeProvider.GetUtcNow().UtcDateTime;
    }
}
