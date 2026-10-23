using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.UnitTests.Users;

public class AccountServiceTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IGroupRepository _groups = Substitute.For<IGroupRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _service = new AccountService(
            _currentUser,
            _users,
            _groups,
            Substitute.For<IEventRepository>(),
            Substitute.For<IAccountQueries>(),
            Substitute.For<IOutbox>(),
            _unitOfWork,
            TimeProvider.System);
    }

    private User SignedInUser()
    {
        var user = User.Create("user@example.com", "Test User", "UTC", AuthProvider.Google, "google-sub-1");
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _currentUser.UserId.Returns(user.Id);
        return user;
    }

    [Fact]
    public async Task UpdateProfile_ChangesNameUsernameAndTimeZoneAndSaves()
    {
        var user = SignedInUser();

        await _service.UpdateProfileAsync("New Name", "new.name", "Europe/Kyiv", CancellationToken.None);

        Assert.Equal("New Name", user.DisplayName);
        Assert.Equal("new.name", user.Username);
        Assert.Equal("Europe/Kyiv", user.TimeZone);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProfile_KeepingTheSameUsernameSkipsTheUniquenessCheck()
    {
        var user = SignedInUser();

        await _service.UpdateProfileAsync("Name", user.Username, "UTC", CancellationToken.None);

        await _users.DidNotReceive().IsUsernameTakenAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EndAllSessions_ChangesSessionVersionAndSaves()
    {
        var user = SignedInUser();
        var before = user.SessionVersion;

        await _service.EndAllSessionsAsync(CancellationToken.None);

        Assert.NotEqual(before, user.SessionVersion);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_RejectsDeletedUser()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<UnauthenticatedException>(() =>
            _service.GetAsync(CancellationToken.None));
        Assert.Equal("auth.user_not_found", exception.Code);
    }
}
