using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

using PlanYourFriday.Application.Users;

namespace PlanYourFriday.Api.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AccountService account,
    IAuthenticationSchemeProvider schemes,
    IWebHostEnvironment environment) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("login/google")]
    public async Task<IActionResult> LoginWithGoogle([FromQuery] string? returnUrl, [FromQuery] string? tz)
    {
        var target = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        if (await schemes.GetSchemeAsync(GoogleDefaults.AuthenticationScheme) is null)
        {
            return environment.IsDevelopment()
                ? Redirect(QueryHelpers.AddQueryString(DevLoginController.Path, new Dictionary<string, string?>
                {
                    ["returnUrl"] = target,
                    ["tz"] = tz,
                }))
                : Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Google sign-in is not configured on this server.");
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = target,
            Items =
            {
                [AuthenticationSetup.TimeZoneItem] = tz
            }
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("me")]
    public Task<UserView> GetMe(CancellationToken cancellationToken)
    {
        return account.GetProfileAsync(cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutEverywhere(CancellationToken cancellationToken)
    {
        await account.EndAllSessionsAsync(cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
