using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Users;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Groups;

public class InvitationEmailTests
{
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();
    private readonly OutboxMessage _message;
    private readonly OutboxDispatcher _dispatcher;
    private readonly Group _group;
    private readonly Invitation _invitation;

    public InvitationEmailTests()
    {
        var organizer = User.Create("org@example.com", "Max", "UTC", AuthProvider.Google, "sub");
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(organizer.Id, Arg.Any<CancellationToken>()).Returns(organizer);

        _group = Group.Create(organizer.Id, "Board games", null, Range(2, 10, 22), TimeSpan.FromHours(2), null, At(1, 0));
        var groups = Substitute.For<IGroupRepository>();
        groups.GetByIdAsync(_group.Id, Arg.Any<CancellationToken>()).Returns(_group);
        _invitation = _group.Invite("friend@example.com");

        _message = new InvitationEmailRequested(_group.Id, _invitation.Id).ToOutboxMessage();
        var store = Substitute.For<IOutboxStore>();
        store.ListDueAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([_message]);

        var handler = new InvitationEmailHandler(
            groups, users, _email, Options.Create(new AppOptions { PublicUrl = new Uri("https://pyf.example") }));
        _dispatcher = new OutboxDispatcher(
            store, [handler], Substitute.For<IUnitOfWork>(), Clock(1, 0), NullLogger<OutboxDispatcher>.Instance);
    }

    [Fact]
    public async Task Dispatch_SendsInvitationWithJoinLinkAndMarksItSent()
    {
        await _dispatcher.DispatchDueAsync(CancellationToken.None);

        await _email.Received(1)
            .SendAsync(
                "friend@example.com",
                Arg.Is<string>(s => s.Contains("Max invited you to \"Board games\"", StringComparison.Ordinal)),
                Arg.Is<string>(b => b.Contains($"https://pyf.example/join/{_group.InviteToken}", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>());
        Assert.Equal(InvitationStatus.Sent, _invitation.Status);
        Assert.NotNull(_message.ProcessedAt);
    }

    [Fact]
    public async Task Dispatch_SkipsRevokedInvitation()
    {
        _group.RevokeInvitation(_invitation.Id);

        await _dispatcher.DispatchDueAsync(CancellationToken.None);

        await _email.DidNotReceiveWithAnyArgs().SendAsync(null!, null!, null!, CancellationToken.None);
        Assert.NotNull(_message.ProcessedAt);
    }

    [Fact]
    public async Task Dispatch_SchedulesRetryWhenSendingFails()
    {
        _email.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("SMTP is down."));

        await _dispatcher.DispatchDueAsync(CancellationToken.None);

        Assert.Null(_message.ProcessedAt);
        Assert.Equal(1, _message.Attempts);
        Assert.Equal(At(1, 0).AddMinutes(1), _message.NextAttemptAt);
    }
}
