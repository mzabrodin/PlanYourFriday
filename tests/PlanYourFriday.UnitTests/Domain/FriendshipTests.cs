using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Friends;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Domain;

public class FriendshipTests
{
    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();

    [Fact]
    public void Request_StoresUsersInCanonicalOrder()
    {
        var a = Friendship.Request(Alice, Bob);
        var b = Friendship.Request(Bob, Alice);

        Assert.Equal(a.UserLowId, b.UserLowId);
        Assert.Equal(a.UserHighId, b.UserHighId);
        Assert.Equal(Bob, a.AddresseeId);
        Assert.Equal(Alice, b.AddresseeId);
        Assert.False(a.IsAccepted);
    }

    [Fact]
    public void Request_RejectsSelf()
    {
        Assert.Equal("friendship.self", Assert.Throws<BusinessRuleViolationException>(() => Friendship.Request(Alice, Alice)).Code);
    }

    [Fact]
    public void Accept_OnlyByAddressee()
    {
        var friendship = Friendship.Request(Alice, Bob);

        Assert.Throws<AccessDeniedException>(() => friendship.Accept(Alice, At(1, 0)));
        friendship.Accept(Bob, At(1, 0));

        Assert.True(friendship.IsAccepted);
        Assert.Equal(At(1, 0), friendship.AcceptedAt);
    }

    [Fact]
    public void OtherThan_ReturnsTheOtherPerson()
    {
        var friendship = Friendship.Befriend(Alice, Bob, At(1, 0));

        Assert.Equal(Bob, friendship.OtherThan(Alice));
        Assert.Equal(Alice, friendship.OtherThan(Bob));
        Assert.Throws<ArgumentException>(() => friendship.OtherThan(Guid.NewGuid()));
    }
}
