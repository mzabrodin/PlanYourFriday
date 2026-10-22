using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

using PlanYourFriday.Api.Auth;
using PlanYourFriday.Application.Calendar;

namespace PlanYourFriday.Api.Calendar;

[ApiController]
[Route("api")]
public sealed class CalendarConnectionsController(
    CalendarConnectionService connections,
    IAuthenticationSchemeProvider schemes) : ControllerBase
{
    [HttpGet("calendar-connections/google/connect")]
    public async Task<IActionResult> ConnectGoogle([FromQuery] string? returnUrl)
    {
        if (await schemes.GetSchemeAsync(AuthenticationSetup.GoogleCalendarScheme) is null)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Google Calendar is not configured on this server.");
        }

        var properties = new GoogleChallengeProperties
        {
            RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/settings",
            AccessType = "offline",
            Prompt = "consent",
            IncludeGrantedScopes = true,
        };
        return Challenge(properties, AuthenticationSetup.GoogleCalendarScheme);
    }

    [HttpGet("calendar-connections")]
    public Task<IReadOnlyList<CalendarConnectionView>> List(CancellationToken cancellationToken)
    {
        return connections.ListAsync(cancellationToken);
    }

    [HttpPatch("calendar-connections/{connectionId:guid}")]
    public Task<CalendarConnectionView> ChooseCalendars(
        Guid connectionId,
        ChooseCalendarsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return connections.ChooseCalendarsAsync(
            connectionId, request.BusyCalendarIds, request.WriteCalendarId, cancellationToken);
    }

    [HttpDelete("calendar-connections/primary")]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        await connections.DisconnectPrimaryAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("groups/{groupId:guid}/availability/import")]
    public async Task<IActionResult> ImportAvailability(Guid groupId, CancellationToken cancellationToken)
    {
        await connections.ImportAvailabilityAsync(groupId, cancellationToken);
        return NoContent();
    }
}

public sealed record ChooseCalendarsRequest(
    [Required]
    [MaxLength(50)]
    IReadOnlyList<string> BusyCalendarIds,
    [Required]
    string WriteCalendarId);
