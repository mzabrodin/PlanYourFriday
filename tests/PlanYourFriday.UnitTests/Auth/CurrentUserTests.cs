using Microsoft.AspNetCore.Http;

using PlanYourFriday.Api.Auth;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.UnitTests.Auth;

public class CurrentUserTests
{
    private static User CreateUser()
    {
        return User.Create("user@example.com", "Test User", "UTC", AuthProvider.Google, "google-sub-1");
    }

    private static CurrentUser For(DefaultHttpContext context)
    {
        return new CurrentUser(new HttpContextAccessor { HttpContext = context });
    }

    [Fact]
    public void UserId_ReturnsIdOfSignedInUser()
    {
        var user = CreateUser();
        var currentUser = For(new DefaultHttpContext { User = CurrentUser.CreatePrincipal(user) });

        Assert.Equal(user.Id, currentUser.UserId);
    }

    [Fact]
    public void UserId_RejectsAnonymousRequest()
    {
        var currentUser = For(new DefaultHttpContext());

        var exception = Assert.Throws<UnauthenticatedException>(() => currentUser.UserId);
        Assert.Equal("auth.not_signed_in", exception.Code);
    }

    [Fact]
    public void IsCurrentSession_FailsAfterEndingAllSessions()
    {
        var user = CreateUser();
        var principal = CurrentUser.CreatePrincipal(user);

        Assert.True(CurrentUser.IsCurrentSession(principal, user));

        user.EndAllSessions();

        Assert.False(CurrentUser.IsCurrentSession(principal, user));
    }
}
