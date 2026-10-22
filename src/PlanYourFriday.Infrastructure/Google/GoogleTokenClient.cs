using System.Net;

using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;

using PlanYourFriday.Application.Calendar;

namespace PlanYourFriday.Infrastructure.Google;

internal sealed class GoogleTokenClient(GoogleAuthorizationCodeFlow flow, TimeProvider timeProvider) : ICalendarTokenClient
{
    private const string FlowUser = "plan-your-friday";

    public async Task<RefreshedToken> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        try
        {
            var token = await flow.RefreshTokenAsync(FlowUser, refreshToken, cancellationToken);
            return new RefreshedToken(
                token.AccessToken,
                token.RefreshToken,
                timeProvider.GetUtcNow().UtcDateTime.AddSeconds(token.ExpiresInSeconds ?? 0));
        }
        catch (TokenResponseException exception) when (exception.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            throw new CalendarAuthorizationException("Google rejected the refresh token.");
        }
        catch (TokenResponseException exception)
        {
            throw new HttpRequestException(exception.Message, exception, exception.StatusCode);
        }
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            await flow.RevokeTokenAsync(FlowUser, token, cancellationToken);
        }
        catch (TokenResponseException exception) when (exception.StatusCode != HttpStatusCode.BadRequest)
        {
            throw new HttpRequestException(exception.Message, exception, exception.StatusCode);
        }
        catch (TokenResponseException)
        {
        }
    }
}
