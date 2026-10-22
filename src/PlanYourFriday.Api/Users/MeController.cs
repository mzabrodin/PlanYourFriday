using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

using PlanYourFriday.Application.Friends;
using PlanYourFriday.Application.Users;

namespace PlanYourFriday.Api.Users;

[ApiController]
[Route("api/me")]
public sealed class MeController(AccountService account) : ControllerBase
{
    [HttpPatch]
    public Task<UserView> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return account.UpdateProfileAsync(request.DisplayName.Trim(), request.Username, request.TimeZone, cancellationToken);
    }

    [HttpGet("settings")]
    public Task<UserSettingsView> GetSettings(CancellationToken cancellationToken)
    {
        return account.GetSettingsAsync(cancellationToken);
    }

    [HttpPut("settings")]
    public Task<UserSettingsView> UpdateSettings(UserSettingsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return account.UpdateSettingsAsync(request.ToSettings(), cancellationToken);
    }

    [HttpGet("export")]
    public Task<AccountExport> Export(CancellationToken cancellationToken)
    {
        return account.ExportAsync(cancellationToken);
    }

    [HttpPost("friend-code")]
    public Task<FriendCodeView> RegenerateFriendCode(CancellationToken cancellationToken)
    {
        return account.RegenerateFriendCodeAsync(cancellationToken);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        await account.DeleteAsync(cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
