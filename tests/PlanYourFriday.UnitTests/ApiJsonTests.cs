using System.Text.Json;

using PlanYourFriday.Api;
using PlanYourFriday.Domain.Groups;

namespace PlanYourFriday.UnitTests;

public class ApiJsonTests
{
    private static JsonSerializerOptions Options()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApiJson.Configure(options);
        return options;
    }

    [Fact]
    public void Enums_AreWrittenAsUpperSnakeCase()
    {
        Assert.Equal("\"PLANNING\"", JsonSerializer.Serialize(GroupStatus.Planning, Options()));
    }

    [Fact]
    public void Enums_AreReadFromUpperSnakeCase()
    {
        Assert.Equal(MemberRole.Participant, JsonSerializer.Deserialize<MemberRole>("\"PARTICIPANT\"", Options()));
    }
}
