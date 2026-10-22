using System.Net;

using Microsoft.Extensions.DependencyInjection;

using PlanYourFriday.Application.Calendar;
using PlanYourFriday.IntegrationTests.Infrastructure;

namespace PlanYourFriday.IntegrationTests;

public class CalendarConnectionTests(ApiFactory api) : ApiTest(api)
{
    [Fact]
    public async Task Disconnect_RevokesAccessAtGoogleAndHidesTheConnection()
    {
        var max = await SignInAsync("Max");
        await ConnectCalendarAsync(max);
        Assert.True((await max.GetAsync("/api/auth/me")).GetProperty("calendarConnected").GetBoolean());

        await max.DeleteAsync("/api/calendar-connections/primary");

        await Api.CalendarTokens.Received(1).RevokeAsync("refresh", Arg.Any<CancellationToken>());
        Assert.Empty((await max.GetAsync("/api/calendar-connections")).EnumerateArray());
        Assert.False((await max.GetAsync("/api/auth/me")).GetProperty("calendarConnected").GetBoolean());
    }

    [Fact]
    public async Task Disconnect_StillWorksWhenGoogleIsUnreachable()
    {
        var max = await SignInAsync("Max");
        await ConnectCalendarAsync(max);
        Api.CalendarTokens.RevokeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new HttpRequestException("Google is down."));

        await max.DeleteAsync("/api/calendar-connections/primary");

        Assert.Empty((await max.GetAsync("/api/calendar-connections")).EnumerateArray());
    }

    [Fact]
    public async Task Disconnect_WithoutConnectionIsNotFound()
    {
        var max = await SignInAsync("Max");

        Assert.Equal(
            "calendar_connection.not_found",
            await CodeOfAsync(max, HttpMethod.Delete, "/api/calendar-connections/primary", HttpStatusCode.NotFound));
    }

    [Fact]
    public async Task ConnectingLater_AddsUpcomingEventsToTheNewCalendar()
    {
        var max = await SignInAsync("Max");
        var group = await CreateGroupAsync(max);
        await max.PostAsync($"/api/groups/{group.GetProperty("id").GetString()}/event", new
        {
            title = "Board games",
            time = Range(18, 20),
            location = "",
        });

        await ConnectCalendarAsync(max);

        var reloaded = await max.GetAsync($"/api/groups/{group.GetProperty("id").GetString()}");
        Assert.Equal("PENDING", reloaded.GetProperty("event").GetProperty("myCalendarSync").GetString());
    }

    [Fact]
    public async Task ExpiredConnection_IsNotRetriedBySync()
    {
        var max = await SignInAsync("Max");
        await ConnectCalendarAsync(max);
        var group = await CreateGroupAsync(max);
        await max.PostAsync($"/api/groups/{group.GetProperty("id").GetString()}/event", new
        {
            title = "Board games",
            time = Range(18, 20),
            location = "",
        });
        Api.CalendarTokens.RefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<RefreshedToken>(_ => throw new CalendarAuthorizationException("Revoked in Google."));
        Api.Clock.Advance(TimeSpan.FromHours(2));

        var first = await SyncAsync();
        Api.Clock.Advance(TimeSpan.FromHours(1));
        var second = await SyncAsync();

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal("EXPIRED", (await max.GetAsync("/api/calendar-connections"))[0].GetProperty("status").GetString());
    }

    private async Task<int> SyncAsync()
    {
        using var scope = Api.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<CalendarSyncService>().SyncPendingAsync(CancellationToken.None);
    }
}
