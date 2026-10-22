using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.IntegrationTests.Contract;
using PlanYourFriday.IntegrationTests.Infrastructure;

namespace PlanYourFriday.IntegrationTests;

public class ContractTests(ApiFactory api) : ApiTest(api)
{
    [Fact]
    public async Task Profile_Settings_FriendCodeAndExport()
    {
        var max = await SignInAsync("Max");

        SpaContract.Verify(await max.GetAsync("/api/auth/me"), "UserDto");
        SpaContract.Verify(
            await max.PatchAsync("/api/me", new { displayName = "Max K", username = "max.k", timeZone = "Europe/Kyiv" }),
            "UserDto");

        var settings = await max.GetAsync("/api/me/settings");
        SpaContract.Verify(settings, "UserSettingsDto");
        SpaContract.Verify(await max.PutAsync("/api/me/settings", settings), "UserSettingsDto");

        SpaContract.Verify(await max.PostAsync("/api/me/friend-code"), "{ friendCode: string }");
        SpaContract.Verify(await max.GetAsync("/api/me/export"), "unknown");
    }

    [Fact]
    public async Task Friends_AndPeople()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        var olia = await SignInAsync("Olia");
        var ivan = await SignInAsync("Ivan");

        SpaContract.Verify(await max.GetAsync($"/api/people/by-code/{tania.FriendCode}"), "PersonDto");
        SpaContract.Verify(await max.PostAsync($"/api/friends/by-code/{tania.FriendCode}"), "PersonDto");
        SpaContract.Verify(await max.PostAsync("/api/friends/requests", new { userId = olia.Id }), "PersonDto");
        await ivan.PostAsync("/api/friends/requests", new { userId = max.Id });
        var suggested = await SignInAsync("Petro");
        var group = await CreateGroupAsync(tania, max);
        await suggested.PostAsync($"/api/groups/join/{group.GetProperty("inviteToken").GetString()}");

        var overview = await max.GetAsync("/api/friends");
        SpaContract.Verify(overview, "FriendsOverviewDto");
        Assert.Single(overview.GetProperty("friends").EnumerateArray());
        Assert.Single(overview.GetProperty("incoming").EnumerateArray());
        Assert.Single(overview.GetProperty("outgoing").EnumerateArray());
        Assert.NotEmpty(overview.GetProperty("suggestions").EnumerateArray());

        var people = await max.GetAsync("/api/people?q=ta");
        SpaContract.Verify(people, "PersonDto[]");
        Assert.NotEmpty(people.EnumerateArray());
    }

    [Fact]
    public async Task Group_FromPlanningToConfirmedEvent()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        var olia = await SignInAsync("Olia");
        await BefriendAsync(max, tania);

        var created = await CreateGroupAsync(max, tania);
        SpaContract.Verify(created, "GroupDto");
        var groupId = created.GetProperty("id").GetString();

        var joined = await olia.PostAsync($"/api/groups/join/{created.GetProperty("inviteToken").GetString()}");
        SpaContract.Verify(joined, "JoinGroupResponse");

        await max.PostAsync($"/api/groups/{groupId}/invitations", new { emails = new[] { "friend@example.com" } });
        await max.PutAsync($"/api/groups/{groupId}/availability", new { slots = new[] { Range(10, 14), Range(18, 22) } });
        await tania.PutAsync($"/api/groups/{groupId}/availability", new { slots = new[] { Range(18, 22) } });

        SpaContract.Verify(await max.GetAsync($"/api/groups/{groupId}"), "GroupDto");
        SpaContract.Verify(await max.GetAsync("/api/groups"), "GroupSummaryDto[]");
        SpaContract.Verify(await max.GetAsync($"/api/groups/{groupId}/availability"), "MemberAvailabilityDto[]");
        SpaContract.Verify(await max.GetAsync($"/api/groups/{groupId}/optimal-slots"), "TimeRange[]");
        var suggestions = await max.GetAsync($"/api/groups/{groupId}/suggestions");
        SpaContract.Verify(suggestions, "SuggestedTimeDto[]");
        Assert.NotEmpty(suggestions.EnumerateArray());
        SpaContract.Verify(
            await max.GetAsync($"/api/calendar?from={Day.AddDays(-1):O}&to={Day.AddDays(2):O}"),
            "CalendarItemDto[]");

        await max.PostAsync($"/api/groups/{groupId}/voting", new
        {
            options = new[] { Range(18, 20), Range(19, 21) },
            deadline = Day.AddHours(12),
        });
        var voting = await tania.GetAsync($"/api/groups/{groupId}");
        var optionId = voting.GetProperty("options")[0].GetProperty("id").GetString();
        await tania.PutAsync($"/api/groups/{groupId}/options/{optionId}/vote", new { answer = "YES" });
        SpaContract.Verify(await max.GetAsync($"/api/groups/{groupId}"), "GroupDto");
        SpaContract.Verify(await tania.GetAsync("/api/groups"), "GroupSummaryDto[]");

        var @event = await max.PostAsync($"/api/groups/{groupId}/event", new
        {
            title = "Board games night",
            time = Range(18, 20),
            location = "Max's place",
            meetingLink = (string?)null,
            description = "Bring snacks",
        });
        SpaContract.Verify(@event, "EventDto");
        var eventId = @event.GetProperty("id").GetString();
        SpaContract.Verify(
            await max.PatchAsync($"/api/events/{eventId}", new
            {
                title = "Board games night",
                location = "Tania's place",
                meetingLink = "https://meet.example.com/abc",
                description = (string?)null,
            }),
            "EventDto");

        SpaContract.Verify(await olia.GetAsync($"/api/groups/{groupId}"), "GroupDto");
        SpaContract.Verify(await olia.GetAsync("/api/groups"), "GroupSummaryDto[]");
        SpaContract.Verify(
            await olia.GetAsync($"/api/calendar?from={Day.AddDays(-1):O}&to={Day.AddDays(2):O}"),
            "CalendarItemDto[]");

        var notifications = await tania.GetAsync("/api/notifications");
        SpaContract.Verify(notifications, "NotificationDto[]");
        Assert.NotEmpty(notifications.EnumerateArray());
    }

    [Fact]
    public async Task Polls_FromCreationToGroup()
    {
        var max = await SignInAsync("Max");
        var tania = await SignInAsync("Tania");
        await BefriendAsync(max, tania);

        var poll = await max.PostAsync("/api/polls", new
        {
            title = "What do we do on Friday?",
            description = (string?)null,
            participantIds = new[] { tania.Id },
            options = new[] { "Bowling", "Pizza" },
            deadline = (DateTime?)null,
            multipleChoice = false,
            allowNewOptions = true,
        });
        SpaContract.Verify(poll, "PollDto");
        var pollId = poll.GetProperty("id").GetString();

        var withOption = await tania.PostAsync($"/api/polls/{pollId}/options", new { title = "Karaoke" });
        SpaContract.Verify(withOption, "PollDto");
        var karaoke = withOption.GetProperty("options")[2].GetProperty("id").GetString();
        SpaContract.Verify(await tania.PutAsync($"/api/polls/{pollId}/votes", new { optionIds = new[] { karaoke } }), "PollDto");
        SpaContract.Verify(await max.GetAsync("/api/polls"), "PollSummaryDto[]");
        SpaContract.Verify(await tania.GetAsync($"/api/polls/{pollId}"), "PollDto");

        var closed = await max.PostAsync($"/api/polls/{pollId}/close");
        SpaContract.Verify(closed, "PollDto");
        Assert.Equal(karaoke, closed.GetProperty("winnerOptionIds")[0].GetString());

        var group = await max.PostAsync("/api/groups", new
        {
            title = "Karaoke",
            description = (string?)null,
            window = Range(9, 23),
            eventDurationMinutes = 120,
            availabilityDeadline = (DateTime?)null,
            memberIds = new[] { tania.Id },
            pollId,
        });
        var planned = await tania.GetAsync($"/api/polls/{pollId}");
        Assert.Equal(group.GetProperty("id").GetString(), planned.GetProperty("groupId").GetString());

        SpaContract.Verify(await tania.GetAsync("/api/notifications"), "NotificationDto[]");
    }

    [Fact]
    public async Task CalendarConnection_ListChooseAndImport()
    {
        var max = await SignInAsync("Max");
        Api.Calendar.ListCalendarsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(
        [
            new ExternalCalendar("primary", "max@example.com", "#4285f4", true),
            new ExternalCalendar("work@group.calendar.google.com", "Work", "#d50000", false),
        ]);
        Api.Calendar.GetBusyAsync(
                Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<TimeRange>(), Arg.Any<CancellationToken>())
            .Returns([new TimeRange(Day.AddHours(12), Day.AddHours(13))]);
        await ConnectCalendarAsync(max);

        var connections = await max.GetAsync("/api/calendar-connections");
        SpaContract.Verify(connections, "CalendarConnectionDto[]");
        var connectionId = connections[0].GetProperty("id").GetString();
        SpaContract.Verify(
            await max.PatchAsync($"/api/calendar-connections/{connectionId}", new
            {
                busyCalendarIds = new[] { "primary", "work@group.calendar.google.com" },
                writeCalendarId = "primary",
            }),
            "CalendarConnectionDto");

        var group = await CreateGroupAsync(max);
        var groupId = group.GetProperty("id").GetString();
        await max.PostAsync($"/api/groups/{groupId}/availability/import");
        var availability = await max.GetAsync($"/api/groups/{groupId}/availability");
        Assert.NotEmpty(availability[0].GetProperty("slots").EnumerateArray());

        var @event = await max.PostAsync($"/api/groups/{groupId}/event", new
        {
            title = "Board games",
            time = Range(18, 20),
            location = "",
            meetingLink = (string?)null,
            description = (string?)null,
        });
        SpaContract.Verify(@event, "EventDto");
        Assert.Equal("PENDING", @event.GetProperty("myCalendarSync").GetString());
    }
}
