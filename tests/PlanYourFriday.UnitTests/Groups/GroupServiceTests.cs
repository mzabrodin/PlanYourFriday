using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Friends;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Polls;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Polls;
using PlanYourFriday.Domain.Users;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Groups;

public class GroupServiceTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IGroupRepository _groups = Substitute.For<IGroupRepository>();
    private readonly IGroupQueries _queries = Substitute.For<IGroupQueries>();
    private readonly IFriendshipRepository _friendships = Substitute.For<IFriendshipRepository>();
    private readonly IPollRepository _polls = Substitute.For<IPollRepository>();
    private readonly IOutbox _outbox = Substitute.For<IOutbox>();
    private readonly INotificationRepository _notifications = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GroupService _service;
    private readonly User _organizer = CreateUser("organizer@example.com");
    private readonly User _friend = CreateUser("friend@example.com");

    public GroupServiceTests()
    {
        foreach (var user in new[] { _organizer, _friend })
        {
            _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        }

        _queries.GetAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => new GroupView(
                call.ArgAt<Guid>(0), "Board games", null, _organizer.Id, null, new TimeRangeView(At(2, 10), At(2, 22)),
                120, null, GroupStatus.Planning, MemberRole.Organizer, [], [], [], null, null));
        _queries.GetMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => new MemberView(call.ArgAt<Guid>(1), "Friend", "friend@example.com", null, MemberRole.Participant, false));
        _friendships.ListFriendIdsAsync(_organizer.Id, Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid> { _friend.Id });

        _service = new GroupService(
            _currentUser,
            new AccountService(_currentUser, _users, _groups, Substitute.For<IAccountQueries>(), _unitOfWork),
            _users,
            _groups,
            _queries,
            _friendships,
            _polls,
            _outbox,
            new NotificationPublisher(_notifications, _outbox),
            _unitOfWork,
            Clock(1, 0));
    }

    private static User CreateUser(string email)
    {
        return User.Create(email, email, "UTC", AuthProvider.Google, email);
    }

    private void SignIn(User user)
    {
        _currentUser.UserId.Returns(user.Id);
    }

    private Group CreateGroup()
    {
        var group = Group.Create(_organizer.Id, "Board games", null, Range(2, 10, 22), TimeSpan.FromHours(2), null, At(1, 0));
        _groups.GetByIdAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);
        _groups.FindByInviteTokenAsync(group.InviteToken, Arg.Any<CancellationToken>()).Returns(group);
        return group;
    }

    private static GroupDraft Draft(IReadOnlyCollection<Guid> memberIds, Guid? pollId = null)
    {
        return new GroupDraft("Board games", null, Range(2, 10, 22), TimeSpan.FromHours(2), null, memberIds, pollId);
    }

    [Fact]
    public async Task Join_AcceptsEmailInvitationOfSignedInUser()
    {
        var group = CreateGroup();
        var invitation = group.Invite("Friend@Example.com");
        SignIn(_friend);

        var joined = await _service.JoinAsync(group.InviteToken, CancellationToken.None);

        Assert.Equal(group.Id, joined.GroupId);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        _notifications.Received(1)
            .Add(Arg.Is<Notification>(n =>
                n.UserId == _organizer.Id && n.Kind == NotificationKind.MemberJoined));
    }

    [Fact]
    public async Task Join_AddsMemberWithoutInvitationAndIsIdempotent()
    {
        var group = CreateGroup();
        SignIn(_friend);

        await _service.JoinAsync(group.InviteToken, CancellationToken.None);
        await _service.JoinAsync(group.InviteToken, CancellationToken.None);

        Assert.Equal(2, group.Members.Count);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Join_RejectsUnknownToken()
    {
        SignIn(_friend);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _service.JoinAsync("unknown", CancellationToken.None));
        Assert.Equal("group.invalid_invite_link", exception.Code);
    }

    [Fact]
    public async Task Invite_QueuesOneEmailPerAddress()
    {
        var group = CreateGroup();
        SignIn(_organizer);

        await _service.InviteAsync(group.Id, ["a@example.com", "A@example.com ", "b@example.com"], CancellationToken.None);

        Assert.Equal(2, group.Invitations.Count);
        _outbox.Received(2).Add(Arg.Is<OutboxMessage>(m => m.Type == InvitationEmailRequested.Type));
    }

    [Fact]
    public async Task Invite_NotifiesExistingUserInAppOnlyBecauseTheInvitationEmailIsSentAnyway()
    {
        var group = CreateGroup();
        _users.FindByEmailAsync(_friend.Email, Arg.Any<CancellationToken>()).Returns(_friend);
        SignIn(_organizer);

        await _service.InviteAsync(group.Id, [_friend.Email], CancellationToken.None);

        _notifications.Received(1).Add(Arg.Is<Notification>(n => n.UserId == _friend.Id && n.Kind == NotificationKind.Invited));
        _outbox.DidNotReceive().Add(Arg.Is<OutboxMessage>(m => m.Type == NotificationEmailRequested.Type));
    }

    [Fact]
    public async Task Create_AddsFriendsAndEmailsThem()
    {
        SignIn(_organizer);
        Group? created = null;
        _groups.When(g => g.Add(Arg.Any<Group>())).Do(call => created = call.Arg<Group>());

        await _service.CreateAsync(Draft([_friend.Id, _organizer.Id]), CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal(2, created.Members.Count);
        _notifications.Received(1).Add(Arg.Is<Notification>(n => n.UserId == _friend.Id && n.Kind == NotificationKind.Invited));
        _outbox.Received(1).Add(Arg.Is<OutboxMessage>(m => m.Type == NotificationEmailRequested.Type));
    }

    [Fact]
    public async Task Create_RejectsPeopleWhoAreNotFriends()
    {
        SignIn(_organizer);

        var exception = await Assert.ThrowsAsync<AccessDeniedException>(() =>
            _service.CreateAsync(Draft([Guid.NewGuid()]), CancellationToken.None));

        Assert.Equal("friendship.not_friends", exception.Code);
        _groups.DidNotReceive().Add(Arg.Any<Group>());
    }

    [Fact]
    public async Task Create_FromClosedPollLinksThePoll()
    {
        SignIn(_organizer);
        var poll = Poll.Create(_organizer.Id, "What to do?", null, [_friend.Id], ["Bowling", "Pizza"], null, false, true, At(1, 0));
        poll.Close(_organizer.Id, At(1, 0));
        _polls.GetByIdAsync(poll.Id, Arg.Any<CancellationToken>()).Returns(poll);

        var view = await _service.CreateAsync(Draft([], poll.Id), CancellationToken.None);

        Assert.Equal(view.Id, poll.GroupId);
    }

    [Fact]
    public async Task AddMembers_AddsOnlyNewFriends()
    {
        var group = CreateGroup();
        SignIn(_organizer);

        await _service.AddMembersAsync(group.Id, [_friend.Id, _organizer.Id], CancellationToken.None);

        Assert.True(group.IsMember(_friend.Id));
        Assert.Equal(2, group.Members.Count);
    }
}
