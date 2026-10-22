using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Users;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Calendar;

public class CalendarAccessTests
{
    private readonly ICalendarTokenClient _oauth = Substitute.For<ICalendarTokenClient>();
    private readonly ITokenProtector _tokens = Substitute.For<ITokenProtector>();
    private readonly CalendarAccess _access;

    public CalendarAccessTests()
    {
        _tokens.Protect(Arg.Any<string>()).Returns(call => "enc:" + call.Arg<string>());
        _tokens.Unprotect(Arg.Any<string>()).Returns(call => call.Arg<string>()["enc:".Length..]);
        _access = new CalendarAccess(_oauth, _tokens, Clock(1, 0));
    }

    private static (User User, CalendarConnection Connection) Connect(DateTime expiresAt)
    {
        var user = User.Create("user@example.com", "User", "UTC", AuthProvider.Google, "sub");
        var connection = user.ConnectCalendar(
            CalendarProvider.Google, "sub", "user@example.com", "primary", "enc:access", "enc:refresh", expiresAt, ["calendar"]);
        return (user, connection);
    }

    [Fact]
    public async Task GetAccessToken_ReturnsStoredTokenWhileValid()
    {
        var (user, connection) = Connect(At(1, 12));

        var token = await _access.GetAccessTokenAsync(user, connection, CancellationToken.None);

        Assert.Equal("access", token);
        await _oauth.DidNotReceive().RefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAccessToken_RefreshesExpiredTokenAndKeepsRefreshToken()
    {
        var (user, connection) = Connect(At(1, 0));
        _oauth.RefreshTokenAsync("refresh", Arg.Any<CancellationToken>())
            .Returns(new RefreshedToken("fresh-access", null, At(2, 0)));

        var token = await _access.GetAccessTokenAsync(user, connection, CancellationToken.None);

        Assert.Equal("fresh-access", token);
        Assert.Equal("enc:fresh-access", connection.AccessToken);
        Assert.Equal("enc:refresh", connection.RefreshToken);
    }

    [Fact]
    public async Task GetAccessToken_MarksConnectionExpiredWhenRefreshIsRejected()
    {
        var (user, connection) = Connect(At(1, 0));
        _oauth.RefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<RefreshedToken>(_ => throw new CalendarAuthorizationException("Rejected."));

        var exception = await Assert.ThrowsAsync<InvalidStateException>(() =>
            _access.GetAccessTokenAsync(user, connection, CancellationToken.None));

        Assert.Equal("calendar_connection.expired", exception.Code);
        Assert.Equal(ConnectionStatus.Expired, connection.Status);
    }
}
