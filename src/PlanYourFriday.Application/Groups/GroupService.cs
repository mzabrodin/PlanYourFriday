using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Friends;
using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Polls;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Notifications;

namespace PlanYourFriday.Application.Groups;

public sealed class GroupService(
    ICurrentUser currentUser,
    AccountService account,
    IUserRepository users,
    IGroupRepository groups,
    IEventRepository events,
    IGroupQueries queries,
    IFriendshipRepository friendships,
    IPollRepository polls,
    IOutbox outbox,
    NotificationPublisher publisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task<IReadOnlyList<GroupSummaryView>> ListAsync(CancellationToken cancellationToken)
    {
        return queries.ListAsync(currentUser.UserId, cancellationToken);
    }

    public async Task<GroupView> GetAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return await queries.GetAsync(groupId, currentUser.UserId, cancellationToken)
            ?? throw new EntityNotFoundException("group.not_found", "Event not found.");
    }

    public async Task<GroupView> CreateAsync(GroupDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var me = currentUser.UserId;
        var group = Group.Create(
            me, draft.Title, draft.Description, draft.Window, draft.EventDuration, draft.AvailabilityDeadline, Now());

        var memberIds = draft.MemberIds.Where(id => id != me).Distinct().ToList();
        await friendships.EnsureFriendsAsync(me, memberIds, cancellationToken);
        foreach (var memberId in memberIds)
        {
            group.AddMember(memberId);
        }

        if (draft.PollId is { } pollId)
        {
            var poll = await polls.GetByIdAsync(pollId, cancellationToken);
            if (poll is null || !poll.IsParticipant(me))
            {
                throw new EntityNotFoundException("poll.not_found", "Poll not found.");
            }

            poll.LinkGroup(me, group.Id, Now());
        }

        groups.Add(group);
        publisher.Notify(memberIds, NotificationKind.Invited, group.Id, me);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetAsync(group.Id, cancellationToken);
    }

    public async Task<JoinGroupView> JoinAsync(string inviteToken, CancellationToken cancellationToken)
    {
        var group = await groups.FindByInviteTokenAsync(inviteToken, cancellationToken)
            ?? throw new EntityNotFoundException("group.invalid_invite_link", "This invite link is invalid or has expired.");
        var user = await account.GetAsync(cancellationToken);

        if (!group.IsMember(user.Id))
        {
            var invitation = group.Invitations.FirstOrDefault(i =>
                string.Equals(i.Email, user.Email, StringComparison.OrdinalIgnoreCase)
                && i.Status is InvitationStatus.Pending or InvitationStatus.Sent);

            if (invitation is not null)
            {
                group.AcceptInvitation(invitation.Id, user.Id);
            }
            else
            {
                group.AddMember(user.Id);
            }

            publisher.NotifyMembers(group, NotificationKind.MemberJoined, user.Id);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var member = await queries.GetMemberAsync(group.Id, user.Id, cancellationToken)
            ?? throw new InvalidOperationException("The member was not found after joining.");
        return new JoinGroupView(group.Id, member);
    }

    public async Task InviteAsync(Guid groupId, IEnumerable<string> emails, CancellationToken cancellationToken)
    {
        var group = (await groups.GetByIdAsync(groupId, cancellationToken)).EnsureMember(currentUser.UserId);
        group.EnsureOrganizer(currentUser.UserId);

        foreach (var email in emails.Select(e => e.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var invitation = group.Invite(email);
            if (invitation.Status == InvitationStatus.Accepted)
            {
                continue;
            }

            outbox.Add(new InvitationEmailRequested(group.Id, invitation.Id).ToOutboxMessage());

            var invitee = await users.FindByEmailAsync(email, cancellationToken);
            if (invitee is not null && !group.IsMember(invitee.Id))
            {
                publisher.NotifyInApp(invitee.Id, NotificationKind.Invited, group.Id, currentUser.UserId);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMembersAsync(Guid groupId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        var me = currentUser.UserId;
        var group = (await groups.GetByIdAsync(groupId, cancellationToken)).EnsureMember(me);
        group.EnsureOrganizer(me);
        var newIds = userIds.Distinct().Where(id => !group.IsMember(id)).ToList();
        await friendships.EnsureFriendsAsync(me, newIds, cancellationToken);
        foreach (var userId in newIds)
        {
            group.AddMember(userId);
        }

        publisher.Notify(newIds, NotificationKind.Invited, group.Id, me);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeInvitationAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var group = await groups.FindByInvitationIdAsync(invitationId, cancellationToken);
        if (group is null || !group.IsMember(currentUser.UserId))
        {
            throw new EntityNotFoundException("invitation.not_found", "Invitation not found.");
        }

        group.EnsureOrganizer(currentUser.UserId);
        group.RevokeInvitation(invitationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemberAvailabilityView>> GetAvailabilityAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var group = await GetAsMemberAsync(groupId, cancellationToken);
        return
        [
            .. group.Members.Select(m => new MemberAvailabilityView(
                m.UserId,
                [.. m.Availability.Select(a => a.Slot).OrderBy(s => s.Start).Select(TimeRangeView.From)])),
        ];
    }

    public async Task SetAvailabilityAsync(
        Guid groupId,
        IEnumerable<TimeRange> slots,
        CancellationToken cancellationToken)
    {
        var group = await GetAsMemberAsync(groupId, cancellationToken);
        group.SetAvailability(currentUser.UserId, slots, Now());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeRangeView>> GetOptimalSlotsAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await GetAsMemberAsync(groupId, cancellationToken);
        return [.. group.FindCommonSlots().Select(TimeRangeView.From)];
    }

    public async Task<IReadOnlyList<SuggestedTimeView>> GetSuggestionsAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await GetAsMemberAsync(groupId, cancellationToken);
        return [.. group.SuggestTimes(Now()).Select(s => new SuggestedTimeView(TimeRangeView.From(s.Time), s.FreeUserIds))];
    }

    public async Task StartVotingAsync(
        Guid groupId,
        IEnumerable<TimeRange> options,
        DateTime? deadline,
        CancellationToken cancellationToken)
    {
        var group = (await groups.GetByIdAsync(groupId, cancellationToken)).EnsureMember(currentUser.UserId);
        group.EnsureOrganizer(currentUser.UserId);
        group.StartVoting(options, deadline, Now());
        publisher.NotifyMembers(group, NotificationKind.VotingStarted, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task VoteAsync(Guid groupId, Guid optionId, VoteAnswer answer, CancellationToken cancellationToken)
    {
        var group = await GetAsMemberAsync(groupId, cancellationToken);
        group.Vote(currentUser.UserId, optionId, answer, Now());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelVotingAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = (await groups.GetByIdAsync(groupId, cancellationToken)).EnsureMember(currentUser.UserId);
        group.EnsureOrganizer(currentUser.UserId);
        group.CancelVoting();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Cancels the plan in any stage. A confirmed event is cancelled too and leaves everyone's calendars.</summary>
    public async Task CancelAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await GetAsMemberAsync(groupId, cancellationToken);
        group.EnsureOrganizer(currentUser.UserId);
        if (await events.FindConfirmedByGroupAsync(group.Id, cancellationToken) is { } @event)
        {
            group.CancelEvent(@event);
        }
        else
        {
            group.Cancel();
        }

        publisher.NotifyMembers(group, NotificationKind.EventCancelled, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Removes a cancelled or finished plan for everyone. Active plans have to be cancelled first.</summary>
    public async Task DeleteAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await GetAsMemberAsync(groupId, cancellationToken);
        group.EnsureOrganizer(currentUser.UserId);
        var groupEvents = await events.ListByGroupAsync(group.Id, cancellationToken);
        var confirmed = groupEvents.FirstOrDefault(e => e.Status == EventStatus.Confirmed);
        var now = Now();
        var over = group.Status == GroupStatus.Cancelled || (confirmed?.Time.End ?? group.Window.End) < now;
        if (!over)
        {
            throw new InvalidStateException("group.delete_active", "Cancel the event before deleting it.");
        }

        CalendarEventRemovalRequested.QueueFor(outbox, groupEvents, now);
        groups.Remove(group);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Group> GetAsMemberAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return (await groups.GetByIdAsync(groupId, cancellationToken)).EnsureMember(currentUser.UserId);
    }

    private DateTime Now()
    {
        return timeProvider.GetUtcNow().UtcDateTime;
    }
}
