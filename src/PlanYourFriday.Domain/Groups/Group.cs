using System.Buffers.Text;
using System.Security.Cryptography;

using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Events;

namespace PlanYourFriday.Domain.Groups;

public sealed class Group : Entity
{
    private const int InviteTokenBytes = 16;
    private const int MaxVotingOptions = 6;

    private readonly List<GroupMember> _members = [];
    private readonly List<Invitation> _invitations = [];
    private readonly List<TimeOption> _options = [];

    private Group()
    {
    }

    private Group(
        Guid organizerId,
        string title,
        string? description,
        TimeRange window,
        TimeSpan eventDuration,
        DateTime? availabilityDeadline,
        DateTime now)
    {
        if (window.End <= now)
        {
            throw new BusinessRuleViolationException("group.window_in_past", "Planning window must not be in the past.");
        }

        if (eventDuration <= TimeSpan.Zero || eventDuration > window.Duration)
        {
            throw new BusinessRuleViolationException("group.invalid_event_duration", "Event duration must be positive and fit into the planning window.");
        }

        if (availabilityDeadline <= now || availabilityDeadline > window.End)
        {
            throw new BusinessRuleViolationException("group.invalid_availability_deadline", "Availability deadline must be in the future and not after the planning window ends.");
        }

        OrganizerId = organizerId;
        Title = title;
        Description = description;
        Window = window;
        EventDuration = eventDuration;
        AvailabilityDeadline = availabilityDeadline;
        Status = GroupStatus.Planning;
        InviteToken = GenerateInviteToken();

        _members.Add(new GroupMember(Id, organizerId, MemberRole.Organizer));
    }

    public Guid OrganizerId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public string InviteToken { get; private set; } = null!;

    public TimeRange Window { get; private set; } = null!;

    public TimeSpan EventDuration { get; private set; }

    public DateTime? AvailabilityDeadline { get; private set; }

    public GroupStatus Status { get; private set; }

    public DateTime? VotingDeadline { get; private set; }

    public IReadOnlyCollection<GroupMember> Members => _members;

    public IReadOnlyCollection<Invitation> Invitations => _invitations;

    public IReadOnlyCollection<TimeOption> Options => _options;

    public static Group Create(
        Guid organizerId,
        string title,
        string? description,
        TimeRange window,
        TimeSpan eventDuration,
        DateTime? availabilityDeadline,
        DateTime now)
    {
        return new Group(organizerId, title, description, window, eventDuration, availabilityDeadline, now);
    }

    public bool IsOrganizer(Guid userId)
    {
        return OrganizerId == userId;
    }

    public bool IsMember(Guid userId)
    {
        return _members.Any(m => m.UserId == userId);
    }

    public void EnsureOrganizer(Guid userId)
    {
        if (!IsOrganizer(userId))
        {
            throw new AccessDeniedException("group.not_organizer", "Only the organizer can do this.");
        }
    }

    public void MarkEventReminderSent(Guid userId, Guid eventId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new EntityNotFoundException("group.member_not_found", "Member not found.");
        member.MarkEventReminderSent(eventId);
    }

    public GroupMember AddMember(Guid userId)
    {
        var existing = _members.FirstOrDefault(m => m.UserId == userId);
        if (existing is not null)
        {
            return existing;
        }

        EnsurePlanning("join");

        var member = new GroupMember(Id, userId, MemberRole.Participant);
        _members.Add(member);
        return member;
    }

    public void RemoveMember(Guid userId)
    {
        if (IsOrganizer(userId))
        {
            throw new InvalidStateException("group.organizer_cannot_leave", "The organizer cannot leave the group.");
        }

        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
        {
            return;
        }

        EnsurePlanning("leave");
        _members.Remove(member);
        foreach (var option in _options)
        {
            option.RemoveVote(userId);
        }
    }

    public bool CanEditAvailability(DateTime now)
    {
        return Status == GroupStatus.Planning && (AvailabilityDeadline is null || now <= AvailabilityDeadline);
    }

    public void SetAvailability(Guid userId, IEnumerable<TimeRange> slots, DateTime now)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new AccessDeniedException("group.not_a_member", "Only group members can set availability.");

        if (!CanEditAvailability(now))
        {
            throw new InvalidStateException("group.availability_closed", "Availability can no longer be edited for this group.");
        }

        var sorted = slots.OrderBy(s => s.Start).ToList();
        if (sorted.Any(s => !Window.Contains(s)))
        {
            throw new BusinessRuleViolationException("group.slot_outside_window", "Availability slots must lie within the planning window.");
        }

        member.SetAvailability(Merge(sorted));
    }

    public IReadOnlyList<TimeRange> FindCommonSlots()
    {
        List<TimeRange> common = [Window];
        common = _members.Aggregate(common, (current, member) => Intersect(current,
            [.. member.Availability.Select(a => a.Slot).OrderBy(s => s.Start)]));

        return [.. common.Where(slot => slot.Duration >= EventDuration)];
    }

    public IReadOnlyList<SuggestedTime> SuggestTimes(DateTime now, int max = 6)
    {
        var step = TimeSpan.FromMinutes(30);
        var from = Window.Start > now ? Window.Start : now;
        var firstStart = new DateTime((from.Ticks + step.Ticks - 1) / step.Ticks * step.Ticks, DateTimeKind.Utc);

        var candidates = new List<SuggestedTime>();
        for (var start = firstStart; start + EventDuration <= Window.End; start += step)
        {
            var time = new TimeRange(start, start + EventDuration);
            var free = _members
                .Where(m => m.Availability.Any(a => a.Slot.Contains(time)))
                .Select(m => m.UserId)
                .ToList();
            if (free.Count > 0)
            {
                candidates.Add(new SuggestedTime(time, free));
            }
        }

        var picked = new List<SuggestedTime>();
        foreach (var candidate in candidates.OrderByDescending(c => c.FreeUserIds.Count).ThenBy(c => c.Time.Start))
        {
            if (picked.Count == max)
            {
                break;
            }

            if (picked.TrueForAll(p => !p.Time.Overlaps(candidate.Time)))
            {
                picked.Add(candidate);
            }
        }

        return picked;
    }

    public Invitation Invite(string email)
    {
        EnsurePlanning("invite to");

        var existing = _invitations.FirstOrDefault(i =>
            string.Equals(i.Email, email, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Reopen();
            return existing;
        }

        var invitation = new Invitation(Id, email);
        _invitations.Add(invitation);
        return invitation;
    }

    public GroupMember AcceptInvitation(Guid invitationId, Guid userId)
    {
        var invitation = FindInvitation(invitationId);
        EnsurePlanning("join");

        invitation.Accept(userId);
        return AddMember(userId);
    }

    public void MarkInvitationSent(Guid invitationId)
    {
        FindInvitation(invitationId).MarkSent();
    }

    public void RevokeInvitation(Guid invitationId)
    {
        FindInvitation(invitationId).Revoke();
    }

    public void RegenerateInviteToken()
    {
        InviteToken = GenerateInviteToken();
    }

    public Event ScheduleEvent(
        string title,
        string? description,
        TimeRange time,
        string? location,
        string? meetingLink,
        DateTime now)
    {
        EnsurePlanning("schedule an event for");

        if (time.Start <= now)
        {
            throw new BusinessRuleViolationException("event.start_in_past", "Event must start in the future.");
        }

        if (!Window.Contains(time))
        {
            throw new BusinessRuleViolationException("event.outside_window", "Event time must lie within the planning window.");
        }

        if (time.Duration < EventDuration)
        {
            throw new BusinessRuleViolationException("event.too_short", "Event must be at least as long as the group's event duration.");
        }

        EndVoting();
        Status = GroupStatus.Scheduled;
        return new Event(Id, OrganizerId, title, description, time, location, meetingLink);
    }

    public void Cancel()
    {
        if (Status == GroupStatus.Scheduled)
        {
            throw new InvalidStateException("group.cancel_scheduled_event_instead", "Cancel the scheduled event instead.");
        }

        Status = GroupStatus.Cancelled;
    }

    public void CancelEvent(Event @event)
    {
        if (@event.GroupId != Id)
        {
            throw new ArgumentException("The event does not belong to this group.", nameof(@event));
        }

        @event.Cancel();
        if (Status == GroupStatus.Scheduled)
        {
            Status = GroupStatus.Planning;
        }
    }

    public void StartVoting(IEnumerable<TimeRange> options, DateTime? deadline, DateTime now)
    {
        if (Status != GroupStatus.Planning)
        {
            throw new InvalidStateException("group.not_planning", $"Cannot start voting in a group that is {Status}.");
        }

        var times = options.Distinct().OrderBy(t => t.Start).ToList();
        if (times.Count is < 1 or > MaxVotingOptions)
        {
            throw new BusinessRuleViolationException("voting.invalid_option_count", $"Pick between 1 and {MaxVotingOptions} times.");
        }

        if (times.Any(t => t.Start <= now || !Window.Contains(t) || t.Duration < EventDuration))
        {
            throw new BusinessRuleViolationException(
                "voting.invalid_option",
                "Every option must be in the future, within the planning window and at least as long as the event.");
        }

        if (deadline <= now || deadline >= times[0].Start)
        {
            throw new BusinessRuleViolationException(
                "voting.invalid_deadline",
                "Voting deadline must be in the future and before the earliest option.");
        }

        _options.Clear();
        _options.AddRange(times.Select(time => new TimeOption(
            Id,
            time,
            _members.Where(m => m.Availability.Any(a => a.Slot.Contains(time))).Select(m => m.UserId))));
        VotingDeadline = deadline;
        Status = GroupStatus.Voting;
    }

    public void Vote(Guid userId, Guid optionId, VoteAnswer answer, DateTime now)
    {
        if (!IsMember(userId))
        {
            throw new AccessDeniedException("group.not_a_member", "Only group members can vote.");
        }

        if (Status != GroupStatus.Voting)
        {
            throw new InvalidStateException("voting.closed", "Voting is closed.");
        }

        if (VotingDeadline < now)
        {
            throw new InvalidStateException("voting.deadline_passed", "The voting deadline has passed.");
        }

        var option = _options.FirstOrDefault(o => o.Id == optionId)
            ?? throw new EntityNotFoundException("voting.option_not_found", "Option not found.");
        option.SetVote(userId, answer);
    }

    public void CancelVoting()
    {
        if (Status != GroupStatus.Voting)
        {
            throw new InvalidStateException("voting.not_started", "There is no vote to cancel.");
        }

        EndVoting();
        Status = GroupStatus.Planning;
    }

    private Invitation FindInvitation(Guid invitationId)
    {
        return _invitations.FirstOrDefault(i => i.Id == invitationId)
            ?? throw new EntityNotFoundException("invitation.not_found", "Invitation not found.");
    }

    private void EndVoting()
    {
        _options.Clear();
        VotingDeadline = null;
    }

    private void EnsurePlanning(string action)
    {
        if (Status is not (GroupStatus.Planning or GroupStatus.Voting))
        {
            throw new InvalidStateException("group.not_planning", $"Cannot {action} a group that is {Status}.");
        }
    }

    private static string GenerateInviteToken()
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(InviteTokenBytes));
    }

    private static List<TimeRange> Intersect(List<TimeRange> left, List<TimeRange> right)
    {
        var result = new List<TimeRange>();
        var i = 0;
        var j = 0;
        while (i < left.Count && j < right.Count)
        {
            var common = left[i].Intersect(right[j]);
            if (common is not null)
            {
                result.Add(common);
            }

            if (left[i].End < right[j].End)
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        return result;
    }

    private static List<TimeRange> Merge(List<TimeRange> sorted)
    {
        var merged = new List<TimeRange>();
        foreach (var slot in sorted)
        {
            var last = merged.Count > 0 ? merged[^1] : null;
            if (last is not null && slot.Start <= last.End)
            {
                merged[^1] = new TimeRange(last.Start, slot.End > last.End ? slot.End : last.End);
            }
            else
            {
                merged.Add(slot);
            }
        }

        return merged;
    }
}
