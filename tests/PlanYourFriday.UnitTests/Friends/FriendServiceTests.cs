using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Friends;
using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Friends;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Users;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Friends;

public class FriendServiceTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IFriendshipRepository _friendships = Substitute.For<IFriendshipRepository>();
    private readonly IFriendQueries _queries = Substitute.For<IFriendQueries>();
    private readonly INotificationRepository _notifications = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FriendService _service;
    private readonly User _me = User.Create("me@example.com", "Me", "UTC", AuthProvider.Google, "me");
    private readonly User _other = User.Create("other@example.com", "Other", "UTC", AuthProvider.Google, "other");

    public FriendServiceTests()
    {
        _currentUser.UserId.Returns(_me.Id);
        _users.GetByIdAsync(_other.Id, Arg.Any<CancellationToken>()).Returns(_other);
        _users.FindByFriendCodeAsync(_other.FriendCode, Arg.Any<CancellationToken>()).Returns(_other);
        _users.FindByFriendCodeAsync(_me.FriendCode, Arg.Any<CancellationToken>()).Returns(_me);
        _queries.GetPersonAsync(_me.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => new PersonView(call.ArgAt<Guid>(1), "Other", "other", Relation.None));

        _service = new FriendService(
            _currentUser,
            _users,
            _friendships,
            _queries,
            new NotificationPublisher(_notifications, Substitute.For<IOutbox>()),
            _unitOfWork,
            Clock(1, 0));
    }

    [Fact]
    public async Task SendRequest_CreatesPendingRequestAndNotifiesTheOtherPerson()
    {
        Friendship? added = null;
        _friendships.When(f => f.Add(Arg.Any<Friendship>())).Do(call => added = call.Arg<Friendship>());

        await _service.SendRequestAsync(_other.Id, CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(FriendshipStatus.Pending, added.Status);
        Assert.Equal(_me.Id, added.RequesterId);
        _notifications.Received(1).Add(Arg.Is<Notification>(n =>
            n.UserId == _other.Id && n.Kind == NotificationKind.FriendRequest));
    }

    [Fact]
    public async Task SendRequest_ToSomeoneWhoAlreadyAskedYouAcceptsTheirRequest()
    {
        var theirs = Friendship.Request(_other.Id, _me.Id);
        _friendships.FindBetweenAsync(_me.Id, _other.Id, Arg.Any<CancellationToken>()).Returns(theirs);

        await _service.SendRequestAsync(_other.Id, CancellationToken.None);

        Assert.True(theirs.IsAccepted);
        _friendships.DidNotReceive().Add(Arg.Any<Friendship>());
        _notifications.Received(1).Add(Arg.Is<Notification>(n => n.Kind == NotificationKind.FriendAccepted));
    }

    [Fact]
    public async Task AddByFriendCode_BefriendsImmediately()
    {
        Friendship? added = null;
        _friendships.When(f => f.Add(Arg.Any<Friendship>())).Do(call => added = call.Arg<Friendship>());

        await _service.AddByFriendCodeAsync($" {_other.FriendCode.ToLowerInvariant()} ", CancellationToken.None);

        Assert.NotNull(added);
        Assert.True(added.IsAccepted);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddByFriendCode_AcceptsOwnPendingRequestToThatPerson()
    {
        var mine = Friendship.Request(_me.Id, _other.Id);
        _friendships.FindBetweenAsync(_me.Id, _other.Id, Arg.Any<CancellationToken>()).Returns(mine);

        await _service.AddByFriendCodeAsync(_other.FriendCode, CancellationToken.None);

        Assert.True(mine.IsAccepted);
    }

    [Fact]
    public async Task AddByFriendCode_RejectsOwnCode()
    {
        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.AddByFriendCodeAsync(_me.FriendCode, CancellationToken.None));

        Assert.Equal("friendship.self", exception.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" @a ")]
    public async Task Search_IgnoresQueriesShorterThanTwoCharacters(string? query)
    {
        var people = await _service.SearchAsync(query, CancellationToken.None);

        Assert.Empty(people);
        await _queries.DidNotReceiveWithAnyArgs().SearchAsync(Guid.Empty, null!, CancellationToken.None);
    }

    [Fact]
    public async Task Search_NormalizesTheQuery()
    {
        await _service.SearchAsync("  @Max ", CancellationToken.None);

        await _queries.Received(1).SearchAsync(_me.Id, "max", Arg.Any<CancellationToken>());
    }
}
