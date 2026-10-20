using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Groups;

public sealed class Invitation : Entity
{
    private Invitation()
    {
    }

    internal Invitation(Guid groupId, string email)
    {
        GroupId = groupId;
        Email = email;
        Status = InvitationStatus.Pending;
    }

    public Guid GroupId { get; private set; }

    public string Email { get; private set; } = null!;

    public InvitationStatus Status { get; private set; }

    public Guid? AcceptedByUserId { get; private set; }

    internal void MarkSent()
    {
        if (Status is not (InvitationStatus.Pending or InvitationStatus.Sent))
        {
            throw new InvalidStateException("invitation.cannot_send", $"Cannot send an invitation that is {Status}.");
        }

        Status = InvitationStatus.Sent;
    }

    internal void Accept(Guid userId)
    {
        if (Status == InvitationStatus.Accepted && AcceptedByUserId == userId)
        {
            return;
        }

        if (Status is not (InvitationStatus.Pending or InvitationStatus.Sent))
        {
            throw new InvalidStateException("invitation.cannot_accept", $"Cannot accept an invitation that is {Status}.");
        }

        Status = InvitationStatus.Accepted;
        AcceptedByUserId = userId;
    }

    internal void Revoke()
    {
        if (Status == InvitationStatus.Accepted)
        {
            throw new InvalidStateException("invitation.cannot_revoke", "Cannot revoke an accepted invitation.");
        }

        Status = InvitationStatus.Revoked;
    }

    internal void Reopen()
    {
        if (Status == InvitationStatus.Revoked)
        {
            Status = InvitationStatus.Pending;
        }
    }
}
