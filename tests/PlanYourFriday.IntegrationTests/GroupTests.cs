using System.Net;

using Microsoft.Extensions.DependencyInjection;

using PlanYourFriday.Application.Outbox;
using PlanYourFriday.IntegrationTests.Infrastructure;

namespace PlanYourFriday.IntegrationTests;

public class GroupTests(ApiFactory api) : ApiTest(api)
{
    [Fact]
    public async Task RevokedInvitation_IsShownAsRevokedAndItsEmailIsNotSent()
    {
        var max = await SignInAsync("Max");
        var group = await CreateGroupAsync(max);
        var groupId = group.GetProperty("id").GetString();
        await max.PostAsync($"/api/groups/{groupId}/invitations", new { emails = new[] { "olia@example.com" } });
        var invitationId = (await max.GetAsync($"/api/groups/{groupId}")).GetProperty("invitations")[0].GetProperty("id").GetString();

        await max.DeleteAsync($"/api/invitations/{invitationId}");

        var invitation = (await max.GetAsync($"/api/groups/{groupId}")).GetProperty("invitations")[0];
        Assert.Equal("REVOKED", invitation.GetProperty("status").GetString());
        using var scope = Api.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>().DispatchDueAsync(CancellationToken.None);
        await Api.Email.DidNotReceiveWithAnyArgs().SendAsync(null!, null!, null!, CancellationToken.None);
    }

    [Fact]
    public async Task RevokeInvitation_ByParticipantIsForbiddenAndUnknownIdIsNotFound()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        var group = await CreateGroupAsync(max);
        var groupId = group.GetProperty("id").GetString();
        await tania.PostAsync($"/api/groups/join/{group.GetProperty("inviteToken").GetString()}");
        await max.PostAsync($"/api/groups/{groupId}/invitations", new { emails = new[] { "olia@example.com" } });
        var invitationId = (await max.GetAsync($"/api/groups/{groupId}")).GetProperty("invitations")[0].GetProperty("id").GetString();

        Assert.Equal(
            "group.not_organizer",
            await CodeOfAsync(tania, HttpMethod.Delete, $"/api/invitations/{invitationId}", HttpStatusCode.Forbidden));
        Assert.Equal(
            "invitation.not_found",
            await CodeOfAsync(max, HttpMethod.Delete, $"/api/invitations/{Guid.NewGuid()}", HttpStatusCode.NotFound));
    }

    [Fact]
    public async Task CancelVoting_ReturnsTheGroupToPlanningWithoutOptions()
    {
        var max = await SignInAsync("Max");
        var group = await CreateGroupAsync(max);
        var groupId = group.GetProperty("id").GetString();
        await max.PostAsync($"/api/groups/{groupId}/voting", new { options = new[] { Range(18, 20) }, deadline = (DateTime?)null });

        await max.DeleteAsync($"/api/groups/{groupId}/voting");

        var reloaded = await max.GetAsync($"/api/groups/{groupId}");
        Assert.Equal("PLANNING", reloaded.GetProperty("status").GetString());
        Assert.Empty(reloaded.GetProperty("options").EnumerateArray());
        Assert.Equal(
            "voting.not_started",
            await CodeOfAsync(max, HttpMethod.Delete, $"/api/groups/{groupId}/voting", HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task VotingAfterCancellation_IsRejected()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        await BefriendAsync(max, tania);
        var group = await CreateGroupAsync(max, tania);
        var groupId = group.GetProperty("id").GetString();
        await max.PostAsync($"/api/groups/{groupId}/voting", new { options = new[] { Range(18, 20) }, deadline = (DateTime?)null });
        var optionId = (await tania.GetAsync($"/api/groups/{groupId}")).GetProperty("options")[0].GetProperty("id").GetString();
        await max.DeleteAsync($"/api/groups/{groupId}/voting");

        Assert.Equal(
            "voting.closed",
            await CodeOfAsync(
                tania, HttpMethod.Put, $"/api/groups/{groupId}/options/{optionId}/vote", HttpStatusCode.Conflict, new { answer = "YES" }));
    }

    [Fact]
    public async Task AddMembers_AddsFriendsAndRejectsStrangers()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        var stranger = await SignInAsync("Stranger");
        await BefriendAsync(max, tania);
        var group = await CreateGroupAsync(max);
        var groupId = group.GetProperty("id").GetString();

        await max.PostAsync($"/api/groups/{groupId}/members", new { userIds = new[] { tania.Id } });

        Assert.Equal(2, (await tania.GetAsync($"/api/groups/{groupId}")).GetProperty("members").GetArrayLength());
        Assert.Equal(
            "friendship.not_friends",
            await CodeOfAsync(
                max, HttpMethod.Post, $"/api/groups/{groupId}/members", HttpStatusCode.Forbidden, new { userIds = new[] { stranger.Id } }));
    }
}
