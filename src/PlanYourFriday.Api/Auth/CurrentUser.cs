using System.Globalization;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.Cookies;

using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Api.Auth;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private const string SessionVersionClaim = "session_version";

    public Guid UserId => FindUserId(httpContextAccessor.HttpContext?.User)
        ?? throw new UnauthenticatedException("auth.not_signed_in", "Sign in to continue.");

    public static ClaimsPrincipal CreatePrincipal(User user)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(SessionVersionClaim, user.SessionVersion.ToString(CultureInfo.InvariantCulture)),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public static Guid? FindUserId(ClaimsPrincipal? principal)
    {
        return principal?.Identity?.IsAuthenticated == true
            && Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                ? userId
                : null;
    }

    public static bool IsCurrentSession(ClaimsPrincipal principal, User user)
    {
        return principal.FindFirstValue(SessionVersionClaim)
            == user.SessionVersion.ToString(CultureInfo.InvariantCulture);
    }
}
