using System.IO.Compression;
using System.Net;
using System.Text;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Http;
using Google.Apis.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Infrastructure;

using GoogleCalendar = Google.Apis.Calendar.v3.CalendarService;

namespace PlanYourFriday.IntegrationTests;

public sealed class GoogleClientsTests : IDisposable
{
    private static readonly DateTime Start = new(2026, 10, 23, 18, 0, 0, DateTimeKind.Utc);

    private readonly FakeGoogle _google = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero));
    private readonly ServiceProvider _services;
    private readonly IExternalCalendarClient _client;
    private readonly ICalendarTokenClient _tokens;

    public GoogleClientsTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Database"] = "Host=unused" })
            .Build();
        var http = new HttpClientFromMessageHandlerFactory(_ =>
            new HttpClientFromMessageHandlerFactory.ConfiguredHttpMessageHandler(_google, false, false));

        _services = new ServiceCollection()
            .AddSingleton<TimeProvider>(_clock)
            .AddInfrastructure(configuration)
            .AddSingleton(new GoogleCalendar(new BaseClientService.Initializer { HttpClientFactory = http }))
            .AddSingleton(new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets { ClientId = "client-id", ClientSecret = "client-secret" },
                HttpClientFactory = http,
            }))
            .BuildServiceProvider();
        _client = _services.GetRequiredService<IExternalCalendarClient>();
        _tokens = _services.GetRequiredService<ICalendarTokenClient>();
    }

    public void Dispose()
    {
        _services.Dispose();
        _google.Dispose();
    }

    private static ExternalEvent Friday()
    {
        return new ExternalEvent("Board games", null, null, null, new TimeRange(Start, Start.AddHours(2)), new Uri("https://app/groups/1"));
    }

    [Fact]
    public async Task SaveEvent_UpdatesTheKnownCopyWithTheUsersToken()
    {
        _google.Reply(HttpStatusCode.OK, """{"id":"evt-1"}""");

        var id = await _client.SaveEventAsync("token-1", "primary", "evt-1", Friday(), CancellationToken.None);

        Assert.Equal("evt-1", id);
        var request = Assert.Single(_google.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.EndsWith("/calendars/primary/events/evt-1", request.Path, StringComparison.Ordinal);
        Assert.Equal("Bearer token-1", request.Authorization);
        Assert.Contains("\"summary\":\"Board games\"", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveEvent_InsertsANewCopyWhenTheOldOneIsGone()
    {
        _google.Reply(HttpStatusCode.NotFound);
        _google.Reply(HttpStatusCode.OK, """{"id":"evt-2"}""");

        var id = await _client.SaveEventAsync("token-1", "primary", "deleted", Friday(), CancellationToken.None);

        Assert.Equal("evt-2", id);
        Assert.Equal([HttpMethod.Put, HttpMethod.Post], _google.Requests.Select(r => r.Method));
    }

    [Fact]
    public async Task DeleteEvent_IgnoresACopyThatIsAlreadyGone()
    {
        _google.Reply(HttpStatusCode.Gone);

        await _client.DeleteEventAsync("token-1", "primary", "evt-1", CancellationToken.None);

        Assert.Equal(HttpMethod.Delete, Assert.Single(_google.Requests).Method);
    }

    [Fact]
    public async Task RejectedAccessToken_AsksTheUserToReconnect()
    {
        _google.Reply(HttpStatusCode.Unauthorized);

        var exception = await Assert.ThrowsAsync<InvalidStateException>(() =>
            _client.ListCalendarsAsync("expired", CancellationToken.None));

        Assert.Equal("calendar_connection.unauthorized", exception.Code);
    }

    [Fact]
    public async Task GetBusy_ReturnsBusyPeriodsOfAllCalendarsInUtc()
    {
        _google.Reply(HttpStatusCode.OK, """
            {"calendars":{
              "primary":{"busy":[{"start":"2026-10-23T21:00:00+03:00","end":"2026-10-23T22:00:00+03:00"}]},
              "work":{"busy":[{"start":"2026-10-23T19:00:00Z","end":"2026-10-23T19:30:00Z"}]}}}
            """);

        var busy = await _client.GetBusyAsync("token-1", ["primary", "work"], new TimeRange(Start, Start.AddHours(6)), CancellationToken.None);

        Assert.Equal([new TimeRange(Start, Start.AddHours(1)), new TimeRange(Start.AddHours(1), Start.AddHours(1.5))], busy);
    }

    [Fact]
    public async Task RefreshToken_ReturnsTheNewTokenAndItsExpiry()
    {
        _google.Reply(HttpStatusCode.OK, """{"access_token":"fresh","expires_in":3600,"token_type":"Bearer"}""");

        var token = await _tokens.RefreshTokenAsync("refresh-1", CancellationToken.None);

        Assert.Equal("fresh", token.AccessToken);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime.AddHours(1), token.ExpiresAt);
        Assert.Contains("grant_type=refresh_token", Assert.Single(_google.Requests).Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefreshToken_RejectedByGoogleIsAnAuthorizationFailure()
    {
        _google.Reply(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""");

        await Assert.ThrowsAsync<CalendarAuthorizationException>(() =>
            _tokens.RefreshTokenAsync("revoked", CancellationToken.None));
    }

    [Fact]
    public async Task Revoke_IgnoresATokenGoogleNoLongerKnows()
    {
        _google.Reply(HttpStatusCode.BadRequest, """{"error":"invalid_token"}""");

        await _tokens.RevokeAsync("old", CancellationToken.None);

        Assert.Single(_google.Requests);
    }

    private sealed record SentRequest(HttpMethod Method, string Path, string? Authorization, string Body);

    private sealed class FakeGoogle : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();

        public List<SentRequest> Requests { get; } = [];

        public void Reply(HttpStatusCode status, string json = "{}")
        {
            _responses.Enqueue(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await ReadBodyAsync(request.Content, cancellationToken);
            Requests.Add(new SentRequest(request.Method, request.RequestUri!.AbsolutePath, request.Headers.Authorization?.ToString(), body));
            return _responses.Dequeue();
        }

        private static async Task<string> ReadBodyAsync(HttpContent? content, CancellationToken cancellationToken)
        {
            if (content is null)
            {
                return string.Empty;
            }

            if (!content.Headers.ContentEncoding.Contains("gzip"))
            {
                return await content.ReadAsStringAsync(cancellationToken);
            }

            await using var gzip = new GZipStream(await content.ReadAsStreamAsync(cancellationToken), CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            return await reader.ReadToEndAsync(cancellationToken);
        }
    }
}
