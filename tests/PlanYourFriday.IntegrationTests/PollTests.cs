using System.Net;

using PlanYourFriday.IntegrationTests.Infrastructure;

namespace PlanYourFriday.IntegrationTests;

public class PollTests(ApiFactory api) : ApiTest(api)
{
    private static async Task<string> CreatePollAsync(TestUser creator, TestUser participant)
    {
        var poll = await creator.PostAsync("/api/polls", new
        {
            title = "What do we do on Friday?",
            description = (string?)null,
            participantIds = new[] { participant.Id },
            options = new[] { "Bowling", "Pizza" },
            deadline = (DateTime?)null,
            multipleChoice = false,
            allowNewOptions = true,
        });
        return poll.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task DeletePoll_RemovesItAndItsNotificationsForEveryone()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        await BefriendAsync(max, tania);
        var pollId = await CreatePollAsync(max, tania);
        var notificationsBefore = (await tania.GetAsync("/api/notifications")).GetArrayLength();

        await max.DeleteAsync($"/api/polls/{pollId}");

        Assert.Equal("poll.not_found", await CodeOfAsync(tania, HttpMethod.Get, $"/api/polls/{pollId}", HttpStatusCode.NotFound));
        Assert.Empty((await tania.GetAsync("/api/polls")).EnumerateArray());
        Assert.Equal(notificationsBefore - 1, (await tania.GetAsync("/api/notifications")).GetArrayLength());
    }

    [Fact]
    public async Task DeletePoll_OnlyTheCreatorCanDelete()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        var olia = await SignInAsync("Olia");
        await BefriendAsync(max, tania);
        var pollId = await CreatePollAsync(max, tania);

        Assert.Equal("poll.not_creator", await CodeOfAsync(tania, HttpMethod.Delete, $"/api/polls/{pollId}", HttpStatusCode.Forbidden));
        Assert.Equal("poll.not_found", await CodeOfAsync(olia, HttpMethod.Delete, $"/api/polls/{pollId}", HttpStatusCode.NotFound));
        await tania.GetAsync($"/api/polls/{pollId}");
    }

    [Fact]
    public async Task ClosedPoll_RejectsVotesAndNewOptions()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        await BefriendAsync(max, tania);
        var pollId = await CreatePollAsync(max, tania);
        var optionId = (await tania.GetAsync($"/api/polls/{pollId}")).GetProperty("options")[0].GetProperty("id").GetString();
        await max.PostAsync($"/api/polls/{pollId}/close");

        Assert.Equal(
            "poll.closed",
            await CodeOfAsync(tania, HttpMethod.Put, $"/api/polls/{pollId}/votes", HttpStatusCode.Conflict, new { optionIds = new[] { optionId } }));
        Assert.Equal(
            "poll.closed",
            await CodeOfAsync(tania, HttpMethod.Post, $"/api/polls/{pollId}/options", HttpStatusCode.Conflict, new { title = "Karaoke" }));
    }
}
