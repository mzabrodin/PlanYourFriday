using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;

using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Api.Auth;

internal static partial class AuthenticationSetup
{
    public const string GoogleCallbackPath = "/api/auth/signin-google";
    public const string GoogleCalendarScheme = "GoogleCalendar";
    public const string GoogleCalendarCallbackPath = "/api/calendar-connections/google/callback";
    public const string TimeZoneItem = "tz";

    private static readonly string[] CalendarScopes =
    [
        "https://www.googleapis.com/auth/calendar.readonly",
        "https://www.googleapis.com/auth/calendar.events",
    ];

    private const string PictureClaim = "picture";

    private static readonly TimeSpan SessionCheckInterval = TimeSpan.FromMinutes(5);

    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authentication = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "pyf_session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context => SetStatusCode(context, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = context => SetStatusCode(context, StatusCodes.Status403Forbidden);
                options.Events.OnValidatePrincipal = ValidateSessionAsync;
            });

        var google = configuration.GetSection("Authentication:Google");
        if (!string.IsNullOrEmpty(google["ClientId"]))
        {
            authentication.AddGoogle(options =>
            {
                google.Bind(options);
                options.CallbackPath = GoogleCallbackPath;
                options.ClaimActions.MapJsonKey(PictureClaim, "picture");
                options.Events.OnCreatingTicket = context => SignInUserAsync(
                    context,
                    AuthProvider.Google,
                    context.User.TryGetProperty("email_verified", out var verified) && verified.ValueKind == JsonValueKind.True);
                options.Events.OnRemoteFailure = context => RedirectWithError(context, "/login?error=");
            });

            authentication.AddGoogle(GoogleCalendarScheme, options =>
            {
                google.Bind(options);
                options.CallbackPath = GoogleCalendarCallbackPath;
                options.AccessType = "offline";
                foreach (var scope in CalendarScopes)
                {
                    options.Scope.Add(scope);
                }

                options.Events.OnCreatingTicket = ConnectCalendarAsync;
                options.Events.OnRemoteFailure = context => RedirectWithError(context, "/settings?calendarError=");
            });
        }

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }

    private static Task SetStatusCode(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }

    private static async Task ValidateSessionAsync(CookieValidatePrincipalContext context)
    {
        var services = context.HttpContext.RequestServices;
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
        if (now - context.Properties.IssuedUtc < SessionCheckInterval)
        {
            return;
        }

        var userId = CurrentUser.FindUserId(context.Principal);
        var user = userId is null
            ? null
            : await services.GetRequiredService<IUserRepository>().GetByIdAsync(userId.Value, context.HttpContext.RequestAborted);

        if (user is null || !CurrentUser.IsCurrentSession(context.Principal!, user))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        context.ShouldRenew = true;
    }

    private static async Task SignInUserAsync(OAuthCreatingTicketContext context, AuthProvider provider, bool emailVerified)
    {
        var principal = context.Principal!;
        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? throw new InvalidOperationException($"{provider} did not return an email address.");
        var login = new ExternalLogin(
            provider,
            principal.FindFirstValue(ClaimTypes.NameIdentifier)!,
            email,
            emailVerified,
            principal.FindFirstValue(ClaimTypes.Name) ?? email,
            principal.FindFirstValue(PictureClaim),
            context.Properties.Items.TryGetValue(TimeZoneItem, out var timeZone) ? timeZone : null);

        var signIn = context.HttpContext.RequestServices.GetRequiredService<ExternalSignInService>();
        var user = await signIn.SignInAsync(login, context.HttpContext.RequestAborted);

        context.Principal = CurrentUser.CreatePrincipal(user);
    }

    private static async Task ConnectCalendarAsync(OAuthCreatingTicketContext context)
    {
        var session = await context.HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var userId = CurrentUser.FindUserId(session.Principal)
            ?? throw new UnauthenticatedException("auth.not_signed_in", "Sign in before connecting a calendar.");

        var granted = context.TokenResponse.Response?.RootElement.TryGetProperty("scope", out var scope) == true
            ? scope.GetString()?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? []
            : [];
        if (CalendarScopes.Any(required => !granted.Contains(required)))
        {
            throw new BusinessRuleViolationException(
                "calendar_connection.missing_scopes",
                "Allow access to Google Calendar to connect it.");
        }

        var services = context.HttpContext.RequestServices;
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var principal = context.Principal!;
        var grant = new GoogleCalendarGrant(
            principal.FindFirstValue(ClaimTypes.NameIdentifier)!,
            principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            context.AccessToken!,
            context.RefreshToken,
            now + (context.ExpiresIn ?? TimeSpan.FromHours(1)),
            granted);
        await services.GetRequiredService<CalendarConnectionService>()
            .ConnectGoogleAsync(userId, grant, context.HttpContext.RequestAborted);

        context.Principal = session.Principal;
    }

    private static Task RedirectWithError(RemoteFailureContext context, string target)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(AuthenticationSetup));
        LogExternalSignInFailed(logger, context.Failure);

        var error = context.Failure is DomainException domainException ? domainException.Code : "sign_in_failed";
        context.Response.Redirect(target + Uri.EscapeDataString(error));
        context.HandleResponse();
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "External sign-in failed")]
    private static partial void LogExternalSignInFailed(ILogger logger, Exception? exception);
}
