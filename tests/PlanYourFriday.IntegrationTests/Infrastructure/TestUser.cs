using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PlanYourFriday.IntegrationTests.Infrastructure;

public sealed class TestUser(HttpClient client, Guid id, string email, string friendCode)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public HttpClient Client { get; } = client;

    public Guid Id { get; } = id;

    public string Email { get; } = email;

    public string FriendCode { get; } = friendCode;

    public Task<JsonElement> GetAsync(string path)
    {
        return SendAsync(HttpMethod.Get, path);
    }

    public Task<JsonElement> PostAsync(string path, object? body = null)
    {
        return SendAsync(HttpMethod.Post, path, body);
    }

    public Task<JsonElement> PutAsync(string path, object body)
    {
        return SendAsync(HttpMethod.Put, path, body);
    }

    public Task<JsonElement> PatchAsync(string path, object body)
    {
        return SendAsync(HttpMethod.Patch, path, body);
    }

    public Task<JsonElement> DeleteAsync(string path)
    {
        return SendAsync(HttpMethod.Delete, path);
    }

    public async Task<JsonElement> SendAsync(HttpMethod method, string path, object? body = null)
    {
        using var response = await SendRawAsync(method, path, body);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{method} {path} returned {(int)response.StatusCode}: {content}");
        }

        return response.StatusCode == HttpStatusCode.NoContent || content.Length == 0
            ? default
            : JsonDocument.Parse(content).RootElement.Clone();
    }

    public async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        return await Client.SendAsync(request);
    }
}
