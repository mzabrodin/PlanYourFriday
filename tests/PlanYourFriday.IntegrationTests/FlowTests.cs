using System.Net;
using System.Text.Json;

using PlanYourFriday.IntegrationTests.Infrastructure;

namespace PlanYourFriday.IntegrationTests;

public class FlowTests(ApiFactory api) : ApiTest(api)
{
    [Fact]
    public async Task AnonymousRequest_GetsProblemJson401()
    {
        using var response = await Anonymous().GetAsync("/api/groups");

        await ProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GroupOfSomeoneElse_IsNotFound()
    {
        var max = await SignInAsync("Max");
        var stranger = await SignInAsync("Stranger");
        var group = await CreateGroupAsync(max);

        using var response = await stranger.SendRawAsync(HttpMethod.Get, $"/api/groups/{group.GetProperty("id").GetString()}");

        var problem = await ProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Equal("group.not_found", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ParticipantCannotInvite()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        var group = await CreateGroupAsync(max);
        await tania.PostAsync($"/api/groups/join/{group.GetProperty("inviteToken").GetString()}");

        using var response = await tania.SendRawAsync(
            HttpMethod.Post,
            $"/api/groups/{group.GetProperty("id").GetString()}/invitations",
            new { emails = new[] { "x@example.com" } });

        var problem = await ProblemAsync(response, HttpStatusCode.Forbidden);
        Assert.Equal("group.not_organizer", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task TakenUsername_IsConflict()
    {
        var max = await SignInAsync("Max");
        await SignInAsync("Tania");

        using var response = await max.SendRawAsync(
            HttpMethod.Patch, "/api/me", new { displayName = "Max", username = "tania", timeZone = "UTC" });

        var problem = await ProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("user.username_taken", problem.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("Max")]
    [InlineData("ma")]
    [InlineData("max!")]
    [InlineData("abcdefghijklmnopqrstuvwxyz12345")]
    public async Task InvalidUsername_IsBadRequest(string username)
    {
        var max = await SignInAsync("Max");

        using var response = await max.SendRawAsync(
            HttpMethod.Patch, "/api/me", new { displayName = "Max", username, timeZone = "UTC" });

        var problem = await ProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty("Username", out _));
    }

    [Fact]
    public async Task CancellingTheEvent_SendsTheGroupBackToPlanning()
    {
        var max = await SignInAsync("Max");
        var group = await CreateGroupAsync(max);
        var groupId = group.GetProperty("id").GetString();
        var @event = await max.PostAsync($"/api/groups/{groupId}/event", new
        {
            title = "Board games",
            time = Range(18, 20),
            location = "",
            meetingLink = (string?)null,
            description = (string?)null,
        });

        await max.DeleteAsync($"/api/events/{@event.GetProperty("id").GetString()}");

        var reloaded = await max.GetAsync($"/api/groups/{groupId}");
        Assert.Equal("PLANNING", reloaded.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, reloaded.GetProperty("event").ValueKind);
    }

    [Fact]
    public async Task Export_ContainsTheUsersDataButNoTokens()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        await max.PostAsync($"/api/friends/by-code/{tania.FriendCode}");
        await CreateGroupAsync(max);

        var export = await max.GetAsync("/api/me/export");
        var text = export.GetRawText();

        Assert.Equal("max", export.GetProperty("profile").GetProperty("username").GetString());
        Assert.Single(export.GetProperty("groups").EnumerateArray());
        Assert.Single(export.GetProperty("friends").EnumerateArray());
        Assert.DoesNotContain("token", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAccount_SignsOutAndRemovesTheUser()
    {
        var max = await SignInAsync("Max");
        await CreateGroupAsync(max);

        await max.DeleteAsync("/api/me");

        using var response = await max.SendRawAsync(HttpMethod.Get, "/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
