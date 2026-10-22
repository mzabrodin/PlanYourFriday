using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Friends;

public sealed class Friendship : Entity
{
    private Friendship()
    {
    }

    private Friendship(Guid requesterId, Guid addresseeId, FriendshipStatus status, DateTime? acceptedAt)
    {
        if (requesterId == addresseeId)
        {
            throw new BusinessRuleViolationException("friendship.self", "You cannot add yourself as a friend.");
        }

        (UserLowId, UserHighId) = Order(requesterId, addresseeId);
        RequesterId = requesterId;
        Status = status;
        AcceptedAt = acceptedAt;
    }

    public Guid UserLowId { get; private set; }

    public Guid UserHighId { get; private set; }

    public Guid RequesterId { get; private set; }

    public FriendshipStatus Status { get; private set; }

    public DateTime? AcceptedAt { get; private set; }

    public Guid AddresseeId => RequesterId == UserLowId ? UserHighId : UserLowId;

    public bool IsAccepted => Status == FriendshipStatus.Accepted;

    public static Friendship Request(Guid requesterId, Guid addresseeId)
    {
        return new Friendship(requesterId, addresseeId, FriendshipStatus.Pending, null);
    }

    public static Friendship Befriend(Guid requesterId, Guid addresseeId, DateTime now)
    {
        return new Friendship(requesterId, addresseeId, FriendshipStatus.Accepted, now);
    }

    public static (Guid Low, Guid High) Order(Guid a, Guid b)
    {
        return a.CompareTo(b) < 0 ? (a, b) : (b, a);
    }

    public bool Involves(Guid userId)
    {
        return UserLowId == userId || UserHighId == userId;
    }

    public Guid OtherThan(Guid userId)
    {
        if (!Involves(userId))
        {
            throw new ArgumentException("The user is not part of this friendship.", nameof(userId));
        }

        return UserLowId == userId ? UserHighId : UserLowId;
    }

    public void Accept(Guid userId, DateTime now)
    {
        if (IsAccepted)
        {
            return;
        }

        if (userId != AddresseeId)
        {
            throw new AccessDeniedException("friendship.not_addressee", "Only the person who received the request can accept it.");
        }

        Status = FriendshipStatus.Accepted;
        AcceptedAt = now;
    }

    public void AcceptByFriendCode(DateTime now)
    {
        if (IsAccepted)
        {
            return;
        }

        Status = FriendshipStatus.Accepted;
        AcceptedAt = now;
    }
}
