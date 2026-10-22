using Microsoft.Extensions.Time.Testing;

using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Friends;
using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Polls;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Polls;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Polls;

public class PollServiceTests
{
    private static readonly Guid Creator = Guid.NewGuid();
    private static readonly Guid Friend = Guid.NewGuid();

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPollRepository _polls = Substitute.For<IPollRepository>();
    private readonly IPollQueries _queries = Substitute.For<IPollQueries>();
    private readonly IFriendshipRepository _friendships = Substitute.For<IFriendshipRepository>();
    private readonly INotificationRepository _notifications = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = Clock(1, 0);
    private readonly PollService _service;

    public PollServiceTests()
    {
        _currentUser.UserId.Returns(Creator);
        _friendships.ListFriendIdsAsync(Creator, Arg.Any<CancellationToken>()).Returns(new HashSet<Guid> { Friend });
        _queries.GetAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call => new PollView(
                call.ArgAt<Guid>(0), "Poll", null, Creator, "Creator", PollStatus.Open, null, false, true, [], [], [], null, At(1, 0)));

        _service = new PollService(
            _currentUser,
            _polls,
            _queries,
            _friendships,
            new NotificationPublisher(_notifications, Substitute.For<IOutbox>()),
            _unitOfWork,
            _clock);
    }

    private Poll StoredPoll(DateTime? deadline = null)
    {
        var poll = Poll.Create(Creator, "What to do?", null, [Friend], ["Bowling", "Pizza"], deadline, false, true, At(1, 0));
        _polls.GetByIdAsync(poll.Id, Arg.Any<CancellationToken>()).Returns(poll);
        return poll;
    }

    private static PollDraft Draft(params Guid[] participantIds)
    {
        return new PollDraft("What to do?", null, participantIds, ["Bowling", "Pizza"], null, false, true);
    }

    [Fact]
    public async Task Create_StoresPollAndNotifiesParticipantsExceptCreator()
    {
        await _service.CreateAsync(Draft(Friend), CancellationToken.None);

        _polls.Received(1).Add(Arg.Is<Poll>(p => p.Participants.Count == 2));
        _notifications.Received(1).Add(Arg.Is<Notification>(n => n.UserId == Friend && n.Kind == NotificationKind.PollStarted));
    }

    [Fact]
    public async Task Create_RejectsParticipantsWhoAreNotFriends()
    {
        var exception = await Assert.ThrowsAsync<AccessDeniedException>(() =>
            _service.CreateAsync(Draft(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("friendship.not_friends", exception.Code);
        _polls.DidNotReceive().Add(Arg.Any<Poll>());
    }

    [Fact]
    public async Task Vote_HidesPollsYouAreNotPartOf()
    {
        var poll = StoredPoll();
        _currentUser.UserId.Returns(Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _service.VoteAsync(poll.Id, [poll.Options.First().Id], CancellationToken.None));

        Assert.Equal("poll.not_found", exception.Code);
    }

    [Fact]
    public async Task Close_AnnouncesOnceEvenWhenCalledTwice()
    {
        var poll = StoredPoll();

        await _service.CloseAsync(poll.Id, CancellationToken.None);
        await _service.CloseAsync(poll.Id, CancellationToken.None);

        Assert.True(poll.ClosedNotified);
        _notifications.Received(1).Add(Arg.Is<Notification>(n => n.UserId == Friend && n.Kind == NotificationKind.PollClosed));
    }

    [Fact]
    public async Task AnnounceClosed_AnnouncesPollsWhoseDeadlinePassed()
    {
        var poll = StoredPoll(deadline: At(2, 0));
        _clock.SetUtcNow(new DateTimeOffset(At(2, 1)));
        _polls.ListClosedUnannouncedAsync(At(2, 1), Arg.Any<CancellationToken>()).Returns([poll]);

        var announced = await _service.AnnounceClosedAsync(CancellationToken.None);

        Assert.Equal(1, announced);
        Assert.True(poll.ClosedNotified);
        _notifications.Received(2).Add(Arg.Is<Notification>(n => n.Kind == NotificationKind.PollClosed));
    }
}
