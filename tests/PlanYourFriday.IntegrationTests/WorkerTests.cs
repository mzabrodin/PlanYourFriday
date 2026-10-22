using Microsoft.Extensions.DependencyInjection;

using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Polls;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.IntegrationTests.Infrastructure;

namespace PlanYourFriday.IntegrationTests;

public class WorkerTests(ApiFactory api) : ApiTest(api)
{
    private async Task<int> RunAsync<TService>(Func<TService, Task<int>> job)
        where TService : notnull
    {
        using var scope = Api.Services.CreateScope();
        return await job(scope.ServiceProvider.GetRequiredService<TService>());
    }

    [Fact]
    public void EveryOutboxMessageTypeHasAHandler()
    {
        using var scope = Api.Services.CreateScope();
        var types = scope.ServiceProvider.GetServices<IOutboxHandler>().Select(h => h.Type).ToList();

        string[] expected = [InvitationEmailRequested.Type, NotificationEmailRequested.Type, EventReminderEmailRequested.Type];
        Assert.Equal(expected.Order(), types.Order());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ReminderService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<PollService>());
    }

    [Fact]
    public async Task Outbox_SendsInvitationAndAddedToGroupEmails()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        await max.PostAsync($"/api/friends/by-code/{tania.FriendCode}");
        var group = await max.PostAsync("/api/groups", new
        {
            title = "Board games",
            window = Range(9, 23),
            eventDurationMinutes = 120,
            memberIds = new[] { tania.Id },
        });
        await max.PostAsync(
            $"/api/groups/{group.GetProperty("id").GetString()}/invitations",
            new { emails = new[] { "olia@example.com" } });

        var dispatched = await RunAsync<OutboxDispatcher>(d => d.DispatchDueAsync(CancellationToken.None));

        Assert.Equal(2, dispatched);
        await Api.Email.Received(1)
            .SendAsync(
                "olia@example.com",
                Arg.Is<string>(s => s.Contains("invited you", StringComparison.Ordinal)),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        await Api.Email.Received(1)
            .SendAsync(
                tania.Email,
                Arg.Is<string>(s => s.Contains("added you", StringComparison.Ordinal)),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        Assert.Equal(
            "SENT",
            (await max.GetAsync($"/api/groups/{group.GetProperty("id").GetString()}")).GetProperty("invitations")[0]
            .GetProperty("status")
            .GetString());
    }

    [Fact]
    public async Task CalendarSync_WritesTheEventIntoTheConnectedCalendar()
    {
        var max = await SignInAsync("Max");
        await ConnectCalendarAsync(max);

        Api.Calendar.SaveEventAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<ExternalEvent>(), Arg.Any<CancellationToken>())
            .Returns("google-event-id");
        var group = await max.PostAsync("/api/groups", new
        {
            title = "Board games",
            window = Range(9, 23),
            eventDurationMinutes = 120,
        });
        var groupId = group.GetProperty("id").GetString();
        await max.PostAsync($"/api/groups/{groupId}/event", new
        {
            title = "Board games",
            time = Range(18, 20),
            location = "Max's place",
        });

        var synced = await RunAsync<CalendarSyncService>(s => s.SyncPendingAsync(CancellationToken.None));

        Assert.Equal(1, synced);
        await Api.Calendar.Received(1)
            .SaveEventAsync(
                "access",
                "primary",
                null,
                Arg.Is<ExternalEvent>(e => e.Title == "Board games" && e.Time == new TimeRange(Day.AddHours(18), Day.AddHours(20))),
                Arg.Any<CancellationToken>());
        var reloaded = await max.GetAsync($"/api/groups/{groupId}");
        Assert.Equal("SYNCED", reloaded.GetProperty("event").GetProperty("myCalendarSync").GetString());
    }
}
