using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Users;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Domain;

public class UserTests
{
    private static User CreateUser()
    {
        return User.Create("user@example.com", "Test User", "Europe/Kyiv", AuthProvider.Google, "google-sub-1");
    }

    private static CalendarConnection Connect(User user, string account, string accessToken = "access")
    {
        return user.ConnectCalendar(
            CalendarProvider.Google, account, $"{account}@example.com", "primary", accessToken, "refresh", At(1, 12), ["calendar"]);
    }

    [Fact]
    public void Create_AddsInitialIdentity()
    {
        var user = CreateUser();

        var identity = Assert.Single(user.Identities);
        Assert.Equal(user.Id, identity.UserId);
        Assert.Equal(AuthProvider.Google, identity.Provider);
        Assert.Equal("google-sub-1", identity.Subject);
    }

    [Fact]
    public void AddIdentity_LinksNewAccountAndReturnsExistingOne()
    {
        var user = CreateUser();

        var existing = user.AddIdentity(AuthProvider.Google, "google-sub-1");
        user.AddIdentity(AuthProvider.Google, "google-sub-2");

        Assert.Same(user.Identities.First(), existing);
        Assert.Equal(2, user.Identities.Count);
    }

    [Fact]
    public void EndAllSessions_ChangesSessionVersion()
    {
        var user = CreateUser();
        var before = user.SessionVersion;

        user.EndAllSessions();

        Assert.NotEqual(before, user.SessionVersion);
    }

    [Fact]
    public void ConnectCalendar_MakesFirstConnectionPrimary()
    {
        var user = CreateUser();

        var first = Connect(user, "account-1");
        var second = Connect(user, "account-2");

        Assert.True(first.IsPrimary);
        Assert.False(second.IsPrimary);
        Assert.Equal(ConnectionStatus.Active, first.Status);
    }

    [Fact]
    public void ConnectCalendar_ReconnectsKnownAccount()
    {
        var user = CreateUser();
        var connection = Connect(user, "account-1");
        user.DisconnectCalendar(connection.Id);

        var reconnected = Connect(user, "account-1", accessToken: "new-access");

        Assert.Same(connection, reconnected);
        Assert.Single(user.CalendarConnections);
        Assert.Equal(ConnectionStatus.Active, reconnected.Status);
        Assert.Equal("new-access", reconnected.AccessToken);
        Assert.True(reconnected.IsPrimary);
    }

    [Fact]
    public void DisconnectCalendar_RevokesConnection()
    {
        var user = CreateUser();
        var connection = Connect(user, "account-1");

        user.DisconnectCalendar(connection.Id);

        Assert.Equal(ConnectionStatus.Revoked, connection.Status);
        Assert.False(connection.IsPrimary);
    }

    [Fact]
    public void DisconnectCalendar_PromotesAnotherActiveConnectionToPrimary()
    {
        var user = CreateUser();
        var primary = Connect(user, "account-1");
        var other = Connect(user, "account-2");

        user.DisconnectCalendar(primary.Id);

        Assert.True(other.IsPrimary);
    }

    [Fact]
    public void DisconnectCalendar_RejectsUnknownConnection()
    {
        var user = CreateUser();

        var exception = Assert.Throws<EntityNotFoundException>(() => user.DisconnectCalendar(Guid.NewGuid()));
        Assert.Equal("calendar_connection.not_found", exception.Code);
    }

    [Fact]
    public void IsExpired_ComparesWithTokenExpiry()
    {
        var connection = Connect(CreateUser(), "account-1");

        Assert.False(connection.IsExpired(At(1, 11)));
        Assert.True(connection.IsExpired(At(1, 12)));
    }

    [Fact]
    public void UpdateCalendarTokens_KeepsRefreshTokenWhenProviderOmitsIt()
    {
        var user = CreateUser();
        var connection = Connect(user, "account-1");
        user.MarkCalendarExpired(connection.Id);

        user.UpdateCalendarTokens(connection.Id, "new-access", null, At(1, 13));

        Assert.Equal("new-access", connection.AccessToken);
        Assert.Equal("refresh", connection.RefreshToken);
        Assert.Equal(At(1, 13), connection.TokenExpiresAt);
        Assert.Equal(ConnectionStatus.Active, connection.Status);
    }

    [Fact]
    public void UpdateCalendarTokens_RejectsRevokedConnection()
    {
        var user = CreateUser();
        var connection = Connect(user, "account-1");
        user.DisconnectCalendar(connection.Id);

        var exception = Assert.Throws<InvalidStateException>(() =>
            user.UpdateCalendarTokens(connection.Id, "new-access", null, At(1, 13)));
        Assert.Equal("calendar_connection.revoked", exception.Code);
    }
}
