using System.Net;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using PlanYourFriday.Application.Calendar;

namespace PlanYourFriday.IntegrationTests.Infrastructure;

[Collection(SharedApi.Name)]
public abstract class ApiTest(ApiFactory api) : IAsyncLifetime, IDisposable
{
    private readonly List<HttpClient> _clients = [];

    protected static DateTime Day { get; } = DateTime.UtcNow.Date.AddDays(3);

    protected ApiFactory Api { get; } = api;

    public Task InitializeAsync()
    {
        return Api.ResetAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    protected static object Range(int fromHour, int toHour)
    {
        return new { start = Day.AddHours(fromHour), end = Day.AddHours(toHour) };
    }

    protected static Task<JsonElement> CreateGroupAsync(TestUser organizer, params TestUser[] members)
    {
        return organizer.PostAsync("/api/groups", new
        {
            title = "Board games",
            description = "Bring snacks",
            window = Range(9, 23),
            eventDurationMinutes = 120,
            availabilityDeadline = Day.AddDays(-1),
            memberIds = members.Select(m => m.Id),
        });
    }

    protected static Task<JsonElement> BefriendAsync(TestUser user, TestUser friend)
    {
        return user.PostAsync($"/api/friends/by-code/{friend.FriendCode}");
    }

    protected async Task ConnectCalendarAsync(TestUser user)
    {
        using var scope = Api.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CalendarConnectionService>().ConnectGoogleAsync(
            user.Id,
            new GoogleCalendarGrant(
                $"google-{user.Id}",
                user.Email,
                "access",
                "refresh",
                Api.Clock.GetUtcNow().UtcDateTime.AddHours(1),
                ["calendar.readonly", "calendar.events"]),
            CancellationToken.None);
    }

    protected HttpClient Anonymous()
    {
        var client = Api.CreateBrowser();
        _clients.Add(client);
        return client;
    }

    protected async Task<TestUser> SignInAsync(string name)
    {
        var email = $"{name.ToLowerInvariant()}@example.com";
        var client = Anonymous();
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = email,
            ["name"] = name,
            ["returnUrl"] = "/",
            ["tz"] = "Europe/Kyiv",
        });

        using var response = await client.PostAsync("/api/auth/dev-login", form);
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect,
            $"Dev sign-in returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        var user = new TestUser(client, Guid.Empty, email, string.Empty);
        var me = await user.GetAsync("/api/auth/me");
        var friends = await user.GetAsync("/api/friends");
        return new TestUser(client, me.GetProperty("id").GetGuid(), email, friends.GetProperty("friendCode").GetString()!);
    }

    protected static async Task<JsonElement> ProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        ArgumentNullException.ThrowIfNull(response);

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"Expected {(int)status}, got {(int)response.StatusCode}: {body}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = JsonDocument.Parse(body).RootElement.Clone();
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));
        return problem;
    }

    protected static async Task<string?> CodeOfAsync(TestUser user, HttpMethod method, string path, HttpStatusCode status, object? body = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        using var response = await user.SendRawAsync(method, path, body);
        return (await ProblemAsync(response, status)).GetProperty("code").GetString();
    }
}
