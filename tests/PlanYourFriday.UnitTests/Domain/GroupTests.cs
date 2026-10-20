using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Groups;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Domain;

public class GroupTests
{
    private static readonly Guid Organizer = Guid.NewGuid();
    private static readonly Guid Participant = Guid.NewGuid();
    private static readonly DateTime Now = At(1, 0);

    private static Group CreateGroup(DateTime? deadline = null)
    {
        return Group.Create(
            Organizer,
            "Friday drinks",
            null,
            new TimeRange(At(2, 0), At(6, 0)),
            TimeSpan.FromHours(2),
            deadline,
            Now);
    }

    [Fact]
    public void Create_AddsOrganizerAsMember()
    {
        var group = CreateGroup();

        var member = Assert.Single(group.Members);
        Assert.Equal(Organizer, member.UserId);
        Assert.Equal(MemberRole.Organizer, member.Role);
        Assert.Equal(group.Id, member.GroupId);
        Assert.True(group.IsOrganizer(Organizer));
        Assert.Equal(GroupStatus.Planning, group.Status);
    }

    [Fact]
    public void Create_RejectsDurationLongerThanWindow()
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(() => Group.Create(
            Organizer, "Trip", null, Range(2, 10, 12), TimeSpan.FromHours(3), null, Now));
        Assert.Equal("group.invalid_event_duration", exception.Code);
    }

    [Fact]
    public void Create_RejectsWindowThatHasEnded()
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(() => Group.Create(
            Organizer, "Trip", null, Range(2, 10, 12), TimeSpan.FromHours(1), null, At(2, 12)));
        Assert.Equal("group.window_in_past", exception.Code);
    }

    [Fact]
    public void Create_AllowsWindowThatHasAlreadyStarted()
    {
        var group = Group.Create(
            Organizer, "Trip", null, Range(2, 10, 18), TimeSpan.FromHours(1), null, At(2, 12));

        Assert.Equal(GroupStatus.Planning, group.Status);
    }

    [Fact]
    public void Create_RejectsDeadlineAfterWindowEnd()
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(() => CreateGroup(deadline: At(7, 0)));
        Assert.Equal("group.invalid_availability_deadline", exception.Code);
    }

    [Fact]
    public void Create_RejectsDeadlineInPast()
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(() => CreateGroup(deadline: Now));
        Assert.Equal("group.invalid_availability_deadline", exception.Code);
    }

    [Fact]
    public void AddMember_IsIdempotent()
    {
        var group = CreateGroup();

        var first = group.AddMember(Participant);
        var second = group.AddMember(Participant);

        Assert.Same(first, second);
        Assert.Equal(MemberRole.Participant, first.Role);
        Assert.Equal(2, group.Members.Count);
    }

    [Fact]
    public void PlanningOnlyOperations_RejectScheduledGroup()
    {
        var group = CreateGroup();
        group.AddMember(Participant);
        group.ScheduleEvent(Range(3, 18, 21), null, null, Now);

        Action[] operations =
        [
            () => group.AddMember(Guid.NewGuid()),
            () => group.RemoveMember(Participant),
            () => group.Invite("friend@example.com"),
            () => group.ScheduleEvent(Range(4, 18, 21), null, null, Now),
        ];

        Assert.All(operations, operation =>
            Assert.Equal("group.not_planning", Assert.Throws<InvalidStateException>(operation).Code));
    }

    [Fact]
    public void RemoveMember_RemovesParticipantWithAvailability()
    {
        var group = CreateGroup();
        group.AddMember(Participant);
        group.SetAvailability(Participant, [Range(2, 10, 12)], Now);

        group.RemoveMember(Participant);

        Assert.False(group.IsMember(Participant));
        Assert.Single(group.Members);
    }

    [Fact]
    public void RemoveMember_RejectsOrganizer()
    {
        var group = CreateGroup();

        var exception = Assert.Throws<InvalidStateException>(() => group.RemoveMember(Organizer));
        Assert.Equal("group.organizer_cannot_leave", exception.Code);
    }

    [Fact]
    public void RemoveMember_IgnoresNonMembers()
    {
        var group = CreateGroup();

        group.RemoveMember(Participant);

        Assert.Single(group.Members);
    }

    [Fact]
    public void CanEditAvailability_RespectsDeadlineAndStatus()
    {
        var group = CreateGroup(deadline: At(3, 0));

        Assert.True(group.CanEditAvailability(At(2, 23)));
        Assert.False(group.CanEditAvailability(At(3, 1)));

        group.Cancel();
        Assert.False(group.CanEditAvailability(At(2, 23)));
    }

    [Fact]
    public void SetAvailability_MergesOverlappingAndAdjacentSlots()
    {
        var group = CreateGroup();
        group.AddMember(Participant);

        group.SetAvailability(
            Participant,
            [Range(3, 14, 16), Range(2, 10, 12), Range(2, 11, 13), Range(2, 13, 14)],
            Now);

        var slots = group.Members.Single(m => m.UserId == Participant).Availability.Select(s => s.Slot);
        Assert.Equal([Range(2, 10, 14), Range(3, 14, 16)], slots);
    }

    [Fact]
    public void SetAvailability_ReplacesPreviousSlots()
    {
        var group = CreateGroup();

        group.SetAvailability(Organizer, [Range(2, 10, 12)], Now);
        group.SetAvailability(Organizer, [Range(4, 9, 11)], Now);

        var slot = Assert.Single(group.Members.Single().Availability);
        Assert.Equal(Range(4, 9, 11), slot.Slot);
    }

    [Fact]
    public void SetAvailability_RejectsSlotsOutsideWindow()
    {
        var group = CreateGroup();

        var exception = Assert.Throws<BusinessRuleViolationException>(() =>
            group.SetAvailability(Organizer, [new TimeRange(At(5, 23), At(6, 1))], Now));
        Assert.Equal("group.slot_outside_window", exception.Code);
    }

    [Fact]
    public void SetAvailability_RejectsNonMembers()
    {
        var group = CreateGroup();

        var exception = Assert.Throws<AccessDeniedException>(() =>
            group.SetAvailability(Participant, [Range(2, 10, 12)], Now));
        Assert.Equal("group.not_a_member", exception.Code);
    }

    [Fact]
    public void SetAvailability_RejectsEditsAfterDeadline()
    {
        var group = CreateGroup(deadline: At(3, 0));

        var exception = Assert.Throws<InvalidStateException>(() =>
            group.SetAvailability(Organizer, [Range(4, 10, 12)], At(3, 1)));
        Assert.Equal("group.availability_closed", exception.Code);
    }

    [Fact]
    public void Invite_IsIdempotentPerEmailIgnoringCase()
    {
        var group = CreateGroup();

        var first = group.Invite("friend@example.com");
        var second = group.Invite("Friend@Example.com");

        Assert.Same(first, second);
        Assert.Single(group.Invitations);
    }

    [Fact]
    public void Invite_ReopensRevokedInvitation()
    {
        var group = CreateGroup();
        var invitation = group.Invite("friend@example.com");
        group.RevokeInvitation(invitation.Id);

        group.Invite("friend@example.com");

        Assert.Equal(InvitationStatus.Pending, invitation.Status);
    }

    [Fact]
    public void RegenerateInviteToken_ChangesToken()
    {
        var group = CreateGroup();
        var oldToken = group.InviteToken;

        group.RegenerateInviteToken();

        Assert.NotEqual(oldToken, group.InviteToken);
    }

    [Fact]
    public void ScheduleEvent_CreatesConfirmedEventOwnedByOrganizer()
    {
        var group = CreateGroup();

        var @event = group.ScheduleEvent(Range(3, 18, 21), "Pub", null, Now);

        Assert.Equal(GroupStatus.Scheduled, group.Status);
        Assert.Equal(group.Id, @event.GroupId);
        Assert.Equal(Organizer, @event.OwnerId);
        Assert.Equal(group.Title, @event.Title);
        Assert.Equal("Pub", @event.Location);
        Assert.Equal(EventStatus.Confirmed, @event.Status);
    }

    [Fact]
    public void ScheduleEvent_RejectsTimeShorterThanEventDuration()
    {
        var group = CreateGroup();

        var exception = Assert.Throws<BusinessRuleViolationException>(() => group.ScheduleEvent(Range(3, 18, 19), null, null, Now));
        Assert.Equal("event.too_short", exception.Code);
    }

    [Fact]
    public void ScheduleEvent_RejectsTimeOutsideWindow()
    {
        var group = CreateGroup();

        var exception = Assert.Throws<BusinessRuleViolationException>(() => group.ScheduleEvent(Range(7, 18, 21), null, null, Now));
        Assert.Equal("event.outside_window", exception.Code);
    }

    [Fact]
    public void ScheduleEvent_RejectsStartInPast()
    {
        var group = CreateGroup();

        var exception = Assert.Throws<BusinessRuleViolationException>(() => group.ScheduleEvent(Range(3, 18, 21), null, null, At(3, 19)));
        Assert.Equal("event.start_in_past", exception.Code);
    }

    [Fact]
    public void Cancel_RejectsScheduledGroup()
    {
        var group = CreateGroup();
        group.ScheduleEvent(Range(3, 18, 21), null, null, Now);

        var exception = Assert.Throws<InvalidStateException>(group.Cancel);
        Assert.Equal("group.cancel_scheduled_event_instead", exception.Code);
    }

    [Fact]
    public void CancelEvent_CancelsEventAndGroupAndQueuesCalendarResync()
    {
        var group = CreateGroup();
        var @event = group.ScheduleEvent(Range(3, 18, 21), null, null, Now);
        var connectionId = Guid.NewGuid();
        var link = @event.AddCalendarLink(connectionId);
        @event.MarkSynced(connectionId, "google-event-id");

        group.CancelEvent(@event);

        Assert.Equal(EventStatus.Cancelled, @event.Status);
        Assert.Equal(GroupStatus.Cancelled, group.Status);
        Assert.Equal(SyncStatus.Pending, link.SyncStatus);
        Assert.Equal("google-event-id", link.ExternalEventId);
    }

    [Fact]
    public void MarkSynced_StoresExternalEventId()
    {
        var @event = CreateGroup().ScheduleEvent(Range(3, 18, 21), null, null, Now);
        var connectionId = Guid.NewGuid();
        var link = @event.AddCalendarLink(connectionId);

        @event.MarkSynced(connectionId, "google-event-id");

        Assert.Equal(SyncStatus.Synced, link.SyncStatus);
        Assert.Equal("google-event-id", link.ExternalEventId);
    }

    [Fact]
    public void MarkSynced_RejectsUnknownConnection()
    {
        var @event = CreateGroup().ScheduleEvent(Range(3, 18, 21), null, null, Now);

        var exception = Assert.Throws<EntityNotFoundException>(() => @event.MarkSynced(Guid.NewGuid(), "google-event-id"));
        Assert.Equal("calendar_link.not_found", exception.Code);
    }

    [Fact]
    public void CancelEvent_RejectsEventOfAnotherGroup()
    {
        var group = CreateGroup();
        var otherEvent = CreateGroup().ScheduleEvent(Range(3, 18, 21), null, null, Now);

        Assert.Throws<ArgumentException>(() => group.CancelEvent(otherEvent));
    }
}
