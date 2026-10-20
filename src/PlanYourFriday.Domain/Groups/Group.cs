using System.Buffers.Text;
using System.Security.Cryptography;

using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Events;

namespace PlanYourFriday.Domain.Groups;

public sealed class Group : Entity
{
    private const int InviteTokenBytes = 16;

    private readonly List<GroupMember> _members = [];
    private readonly List<Invitation> _invitations = [];

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

    public IReadOnlyCollection<GroupMember> Members => _members;

    public IReadOnlyCollection<Invitation> Invitations => _invitations;

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

    public Event ScheduleEvent(TimeRange time, string? location, string? meetingLink, DateTime now)
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

        Status = GroupStatus.Scheduled;
        return new Event(Id, OrganizerId, Title, Description, time, location, meetingLink);
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
        Status = GroupStatus.Cancelled;
    }

    private Invitation FindInvitation(Guid invitationId)
    {
        return _invitations.FirstOrDefault(i => i.Id == invitationId)
            ?? throw new EntityNotFoundException("invitation.not_found", "Invitation not found.");
    }

    private void EnsurePlanning(string action)
    {
        if (Status != GroupStatus.Planning)
        {
            throw new InvalidStateException("group.not_planning", $"Cannot {action} a group that is {Status}.");
        }
    }

    private static string GenerateInviteToken()
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(InviteTokenBytes));
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
