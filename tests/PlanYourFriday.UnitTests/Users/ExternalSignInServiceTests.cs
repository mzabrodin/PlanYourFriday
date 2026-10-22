using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.UnitTests.Users;

public class ExternalSignInServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ExternalSignInService _service;

    public ExternalSignInServiceTests()
    {
        _users.ListUsernamesStartingWithAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());
        _service = new ExternalSignInService(_users, _unitOfWork);
    }

    private static ExternalLogin Login(
        string subject,
        bool emailVerified = true,
        string? timeZone = null,
        string? photoUrl = null)
    {
        return new ExternalLogin(
            AuthProvider.Google, subject, "user@example.com", emailVerified, "Test User", photoUrl, timeZone);
    }

    private static User ExistingUser()
    {
        return User.Create("user@example.com", "Test User", "UTC", AuthProvider.Google, "google-sub-1");
    }

    [Fact]
    public async Task SignIn_CreatesUserForNewAccount()
    {
        var user = await _service.SignInAsync(Login("google-sub-1", timeZone: "Europe/Kyiv"), CancellationToken.None);

        _users.Received(1).Add(user);
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal("Test User", user.DisplayName);
        Assert.Equal("Europe/Kyiv", user.TimeZone);
        Assert.Equal("user", user.Username);
        Assert.Equal("google-sub-1", Assert.Single(user.Identities).Subject);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Mars/Olympus")]
    public async Task SignIn_FallsBackToUtcForMissingOrUnknownTimeZone(string? timeZone)
    {
        var user = await _service.SignInAsync(Login("google-sub-1", timeZone: timeZone), CancellationToken.None);

        Assert.Equal(ExternalSignInService.DefaultTimeZone, user.TimeZone);
    }

    [Fact]
    public async Task SignIn_PicksTheFirstFreeUsername()
    {
        _users.ListUsernamesStartingWithAsync("user", Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "user", "user2" });

        var user = await _service.SignInAsync(Login("google-sub-1"), CancellationToken.None);

        Assert.Equal("user3", user.Username);
    }

    [Fact]
    public async Task SignIn_ReturnsUserOfKnownAccountAndUpdatesPhoto()
    {
        var existing = ExistingUser();
        _users.FindByIdentityAsync(AuthProvider.Google, "google-sub-1", Arg.Any<CancellationToken>()).Returns(existing);

        var user = await _service.SignInAsync(Login("google-sub-1", photoUrl: "https://photo"), CancellationToken.None);

        Assert.Same(existing, user);
        Assert.Equal("https://photo", user.PhotoUrl);
        _users.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task SignIn_LinksNewAccountWithVerifiedEmailToExistingUser()
    {
        var existing = ExistingUser();
        _users.FindByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(existing);

        var user = await _service.SignInAsync(Login("google-sub-2"), CancellationToken.None);

        Assert.Same(existing, user);
        Assert.Equal(2, user.Identities.Count);
    }

    [Fact]
    public async Task SignIn_RejectsNewAccountWithUnverifiedEmailOfExistingUser()
    {
        _users.FindByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(ExistingUser());

        var exception = await Assert.ThrowsAsync<AlreadyExistsException>(() =>
            _service.SignInAsync(Login("google-sub-2", emailVerified: false), CancellationToken.None));

        Assert.Equal("user.email_taken", exception.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
