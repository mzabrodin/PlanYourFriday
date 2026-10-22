using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlanYourFriday.Api;

internal static class ApiJson
{
    public static void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
    }
}
