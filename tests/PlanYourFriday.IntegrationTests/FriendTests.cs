using System.Net;
using System.Text.Json;

using PlanYourFriday.IntegrationTests.Infrastructure;

namespace PlanYourFriday.IntegrationTests;

public class FriendTests(ApiFactory api) : ApiTest(api)
{
    private static async Task<string> SendRequestAsync(TestUser from, TestUser to)
    {
        await from.PostAsync("/api/friends/requests", new { userId = to.Id });
        var incoming = (await to.GetAsync("/api/friends")).GetProperty("incoming");
        return incoming[0].GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> OverviewAsync(TestUser user)
    {
        return await user.GetAsync("/api/friends");
    }

    [Fact]
    public async Task AcceptRequest_MakesBothFriendsAndNotifiesTheRequester()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        var requestId = await SendRequestAsync(max, tania);

        await tania.PostAsync($"/api/friends/requests/{requestId}/accept");

        Assert.Equal(1, (await OverviewAsync(max)).GetProperty("friends").GetArrayLength());
        Assert.Equal(1, (await OverviewAsync(tania)).GetProperty("friends").GetArrayLength());
        var notifications = await max.GetAsync("/api/notifications");
        Assert.Contains(notifications.EnumerateArray(), n => n.GetProperty("kind").GetString() == "FRIEND_ACCEPTED");
    }

    [Fact]
    public async Task AcceptRequest_OnlyTheAddresseeCanAccept()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        var requestId = await SendRequestAsync(max, tania);

        Assert.Equal(
            "friendship.not_addressee",
            await CodeOfAsync(max, HttpMethod.Post, $"/api/friends/requests/{requestId}/accept", HttpStatusCode.Forbidden));
    }

    [Fact]
    public async Task RemoveRequest_WorksForBothSidesAndHidesItFromOthers()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        var olia = await SignInAsync("Olia");
        var requestId = await SendRequestAsync(max, tania);

        Assert.Equal(
            "friend_request.not_found",
            await CodeOfAsync(olia, HttpMethod.Delete, $"/api/friends/requests/{requestId}", HttpStatusCode.NotFound));

        await tania.DeleteAsync($"/api/friends/requests/{requestId}");

        Assert.Empty((await OverviewAsync(tania)).GetProperty("incoming").EnumerateArray());
        Assert.Empty((await OverviewAsync(max)).GetProperty("outgoing").EnumerateArray());
    }

    [Fact]
    public async Task RemoveRequest_CannotUndoAnAcceptedFriendship()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        var requestId = await SendRequestAsync(max, tania);
        await tania.PostAsync($"/api/friends/requests/{requestId}/accept");

        Assert.Equal(
            "friendship.already_friends",
            await CodeOfAsync(max, HttpMethod.Delete, $"/api/friends/requests/{requestId}", HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task RemoveFriend_EndsTheFriendshipForBothAndIsIdempotent()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        await BefriendAsync(max, tania);

        await max.DeleteAsync($"/api/friends/{tania.Id}");
        await max.DeleteAsync($"/api/friends/{tania.Id}");

        Assert.Empty((await OverviewAsync(max)).GetProperty("friends").EnumerateArray());
        Assert.Empty((await OverviewAsync(tania)).GetProperty("friends").EnumerateArray());
        Assert.Equal(
            "friendship.not_friends",
            await CodeOfAsync(max, HttpMethod.Post, "/api/groups", HttpStatusCode.Forbidden, new
            {
                title = "Board games",
                window = Range(9, 23),
                eventDurationMinutes = 120,
                memberIds = new[] { tania.Id },
            }));
    }

    [Theory]
    [InlineData("a_")]
    [InlineData("%a")]
    [InlineData("t%a")]
    public async Task SearchPeople_TreatsWildcardsAsPlainText(string query)
    {
        var max = await SignInAsync("Max");
        await SignInAsync("Tania");

        var people = await max.GetAsync($"/api/people?q={Uri.EscapeDataString(query)}");

        Assert.Empty(people.EnumerateArray());
    }

    [Fact]
    public async Task SearchPeople_MatchesDisplayNameIgnoringCaseAndListsUsernamePrefixFirst()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        var olia = await SignInAsync("Olia");
        await tania.PatchAsync("/api/me", new { displayName = "Tetiana Koval", username = "tania", timeZone = "UTC" });
        await olia.PatchAsync("/api/me", new { displayName = "Olia Tanenko", username = "olia", timeZone = "UTC" });

        var byDisplayName = await max.GetAsync("/api/people?q=KOVAL");
        var byPrefix = await max.GetAsync("/api/people?q=tan");

        Assert.Equal("tania", Assert.Single(byDisplayName.EnumerateArray()).GetProperty("username").GetString());
        Assert.Equal(["tania", "olia"], byPrefix.EnumerateArray().Select(p => p.GetProperty("username").GetString()));
    }

    [Fact]
    public async Task RegenerateFriendCode_InvalidatesTheOldLink()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");

        var regenerated = await tania.PostAsync("/api/me/friend-code");

        Assert.NotEqual(tania.FriendCode, regenerated.GetProperty("friendCode").GetString());
        Assert.Equal(
            "friend_code.not_found",
            await CodeOfAsync(max, HttpMethod.Post, $"/api/friends/by-code/{tania.FriendCode}", HttpStatusCode.NotFound));
    }
}
