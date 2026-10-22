using System.Net;

using PlanYourFriday.IntegrationTests.Infrastructure;

namespace PlanYourFriday.IntegrationTests;

public class AuthTests(ApiFactory api) : ApiTest(api)
{
    [Fact]
    public async Task Logout_EndsOnlyThisBrowsersSession()
    {
        var laptop = await SignInAsync("Max");
        var phone = await SignInAsync("Max");

        await laptop.PostAsync("/api/auth/logout");

        using var signedOut = await laptop.SendRawAsync(HttpMethod.Get, "/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, signedOut.StatusCode);
        await phone.GetAsync("/api/auth/me");
    }

    [Fact]
    public async Task LogoutEverywhere_EndsOtherSessionsAtTheirNextCheck()
    {
        var laptop = await SignInAsync("Max");
        var phone = await SignInAsync("Max");

        await laptop.PostAsync("/api/auth/logout-all");
        Api.Clock.Advance(TimeSpan.FromMinutes(6));

        using var laptopResponse = await laptop.SendRawAsync(HttpMethod.Get, "/api/auth/me");
        using var phoneResponse = await phone.SendRawAsync(HttpMethod.Get, "/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, laptopResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, phoneResponse.StatusCode);
    }

    [Fact]
    public async Task SessionSurvivesItsPeriodicCheckWhileNothingChanged()
    {
        var max = await SignInAsync("Max");

        Api.Clock.Advance(TimeSpan.FromMinutes(6));

        await max.GetAsync("/api/auth/me");
    }

    [Theory]
    [InlineData("/groups/42", "%2Fgroups%2F42")]
    [InlineData("https://evil.example/steal", "%2F")]
    [InlineData("//evil.example", "%2F")]
    public async Task LoginWithoutGoogle_RedirectsToDevSignInWithALocalReturnUrl(string returnUrl, string expected)
    {
        using var response = await Anonymous()
            .GetAsync(
                $"/api/auth/login/google?returnUrl={Uri.EscapeDataString(returnUrl)}&tz=Europe%2FKyiv");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.OriginalString;
        Assert.StartsWith("/api/auth/dev-login?", location, StringComparison.Ordinal);
        Assert.Contains($"returnUrl={expected}", location, StringComparison.Ordinal);
        Assert.Contains("tz=Europe%2FKyiv", location, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DevSignInForm_IsServedInDevelopment()
    {
        using var response = await Anonymous().GetAsync("/api/auth/dev-login?returnUrl=%2Fgroups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("value=\"/groups\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConnectCalendarWithoutGoogle_IsServiceUnavailable()
    {
        var max = await SignInAsync("Max");

        using var response = await max.SendRawAsync(HttpMethod.Get, "/api/calendar-connections/google/connect?returnUrl=%2Fsettings");

        await ProblemAsync(response, HttpStatusCode.ServiceUnavailable);
    }
}
