using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Groups;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Domain;

public class InvitationTests
{
    private static readonly DateTime Now = At(1, 0);

    private static (Group Group, Invitation Invitation) CreateInvitation()
    {
        var group = Group.Create(
            Guid.NewGuid(), "Board games", null, Range(2, 10, 22), TimeSpan.FromHours(2), null, Now);
        return (group, group.Invite("friend@example.com"));
    }

    [Fact]
    public void MarkInvitationSent_RejectsRevokedInvitation()
    {
        var (group, invitation) = CreateInvitation();
        group.RevokeInvitation(invitation.Id);

        var exception = Assert.Throws<InvalidStateException>(() => group.MarkInvitationSent(invitation.Id));
        Assert.Equal("invitation.cannot_send", exception.Code);
    }

    [Fact]
    public void AcceptInvitation_AcceptsAndAddsMember()
    {
        var (group, invitation) = CreateInvitation();
        var userId = Guid.NewGuid();
        group.MarkInvitationSent(invitation.Id);

        var member = group.AcceptInvitation(invitation.Id, userId);

        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.Equal(userId, invitation.AcceptedByUserId);
        Assert.Equal(userId, member.UserId);
        Assert.Equal(MemberRole.Participant, member.Role);
        Assert.True(group.IsMember(userId));
    }

    [Fact]
    public void AcceptInvitation_IsIdempotentForSameUser()
    {
        var (group, invitation) = CreateInvitation();
        var userId = Guid.NewGuid();

        var first = group.AcceptInvitation(invitation.Id, userId);
        var second = group.AcceptInvitation(invitation.Id, userId);

        Assert.Same(first, second);
        Assert.Equal(2, group.Members.Count);
    }

    [Fact]
    public void AcceptInvitation_RejectsInvitationAcceptedByAnotherUser()
    {
        var (group, invitation) = CreateInvitation();
        group.AcceptInvitation(invitation.Id, Guid.NewGuid());

        var exception = Assert.Throws<InvalidStateException>(() => group.AcceptInvitation(invitation.Id, Guid.NewGuid()));
        Assert.Equal("invitation.cannot_accept", exception.Code);
    }

    [Fact]
    public void AcceptInvitation_RejectsRevokedInvitationWithoutAddingMember()
    {
        var (group, invitation) = CreateInvitation();
        var userId = Guid.NewGuid();
        group.RevokeInvitation(invitation.Id);

        var exception = Assert.Throws<InvalidStateException>(() => group.AcceptInvitation(invitation.Id, userId));
        Assert.Equal("invitation.cannot_accept", exception.Code);
        Assert.False(group.IsMember(userId));
    }

    [Fact]
    public void AcceptInvitation_RejectsScheduledGroup()
    {
        var (group, invitation) = CreateInvitation();
        group.ScheduleEvent("Event", null, Range(2, 18, 21), null, null, Now);

        var exception = Assert.Throws<InvalidStateException>(() => group.AcceptInvitation(invitation.Id, Guid.NewGuid()));
        Assert.Equal("group.not_planning", exception.Code);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
    }

    [Fact]
    public void AcceptInvitation_RejectsUnknownInvitation()
    {
        var (group, _) = CreateInvitation();

        var exception = Assert.Throws<EntityNotFoundException>(() => group.AcceptInvitation(Guid.NewGuid(), Guid.NewGuid()));
        Assert.Equal("invitation.not_found", exception.Code);
    }

    [Fact]
    public void RevokeInvitation_RejectsAcceptedInvitation()
    {
        var (group, invitation) = CreateInvitation();
        group.AcceptInvitation(invitation.Id, Guid.NewGuid());

        var exception = Assert.Throws<InvalidStateException>(() => group.RevokeInvitation(invitation.Id));
        Assert.Equal("invitation.cannot_revoke", exception.Code);
    }
}
