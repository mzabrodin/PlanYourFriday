using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Api.Auth;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route(Path)]
public sealed class DevLoginController(ExternalSignInService signIn, IWebHostEnvironment environment) : ControllerBase
{
    public const string Path = "/api/auth/dev-login";

    [HttpGet]
    public IActionResult Form([FromQuery] string? returnUrl, [FromQuery] string? tz)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        return Content(DevLoginPage.Render(Path, SafeReturnUrl(returnUrl), tz), "text/html");
    }

    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> SignIn([FromForm] DevLoginForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var email = form.Email.Trim().ToLowerInvariant();
        var login = new ExternalLogin(
            AuthProvider.Development,
            email,
            email,
            true,
            string.IsNullOrWhiteSpace(form.Name) ? email.Split('@')[0] : form.Name.Trim(),
            null,
            form.Tz);
        var user = await signIn.SignInAsync(login, cancellationToken);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CurrentUser.CreatePrincipal(user),
            new AuthenticationProperties { IsPersistent = true });
        return LocalRedirect(SafeReturnUrl(form.ReturnUrl));
    }

    private string SafeReturnUrl(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
    }
}

public sealed record DevLoginForm(
    [Required]
    [EmailAddress]
    string Email,
    string? Name,
    string? ReturnUrl,
    string? Tz);
