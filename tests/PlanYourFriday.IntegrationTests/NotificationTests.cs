using System.Text.Json;

using PlanYourFriday.IntegrationTests.Infrastructure;

namespace PlanYourFriday.IntegrationTests;

public class NotificationTests(ApiFactory api) : ApiTest(api)
{
    private static List<JsonElement> Items(JsonElement notifications)
    {
        return [.. notifications.EnumerateArray()];
    }

    private async Task<TestUser> UserWithTwoNotificationsAsync()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        await BefriendAsync(max, tania);
        await CreateGroupAsync(max, tania);
        return tania;
    }

    [Fact]
    public async Task MarkRead_WithIdsMarksOnlyThose()
    {
        var tania = await UserWithTwoNotificationsAsync();
        var before = Items(await tania.GetAsync("/api/notifications"));
        Assert.Equal(2, before.Count);
        Assert.All(before, n => Assert.False(n.GetProperty("read").GetBoolean()));

        await tania.PostAsync("/api/notifications/read", new { ids = new[] { before[0].GetProperty("id").GetString() } });

        var after = Items(await tania.GetAsync("/api/notifications"));
        Assert.Equal(1, after.Count(n => n.GetProperty("read").GetBoolean()));
        Assert.True(after.Single(n => n.GetProperty("id").GetString() == before[0].GetProperty("id").GetString())
            .GetProperty("read")
            .GetBoolean());
    }

    [Fact]
    public async Task MarkRead_WithoutIdsMarksEverything()
    {
        var tania = await UserWithTwoNotificationsAsync();

        await tania.PostAsync("/api/notifications/read", new { ids = (string[]?)null });

        Assert.All(Items(await tania.GetAsync("/api/notifications")), n => Assert.True(n.GetProperty("read").GetBoolean()));
    }

    [Fact]
    public async Task MarkRead_IgnoresOtherPeoplesNotifications()
    {
        var tania = await UserWithTwoNotificationsAsync();
        var olia = await SignInAsync("Olia");
        var taniasIds = Items(await tania.GetAsync("/api/notifications")).Select(n => n.GetProperty("id").GetString()).ToArray();

        await olia.PostAsync("/api/notifications/read", new { ids = taniasIds });

        Assert.All(Items(await tania.GetAsync("/api/notifications")), n => Assert.False(n.GetProperty("read").GetBoolean()));
    }
}
