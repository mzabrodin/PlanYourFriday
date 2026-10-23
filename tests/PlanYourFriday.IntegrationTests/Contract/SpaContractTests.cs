using System.Text.Json;

using Xunit.Sdk;

namespace PlanYourFriday.IntegrationTests.Contract;

public class SpaContractTests
{
    private const string ValidPerson = """{"userId":"1","displayName":"Max","username":"max","photoUrl":null,"relation":"FRIEND"}""";

    private static JsonElement Json(string text)
    {
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    [Fact]
    public void AcceptsMatchingResponse()
    {
        SpaContract.Verify(Json(ValidPerson), "PersonDto");
        SpaContract.Verify(Json($"[{ValidPerson}]"), "PersonDto[]");
    }

    [Theory]
    [InlineData("""{"userId":"1","displayName":"Max","photoUrl":null,"relation":"FRIEND"}""", "$.username: missing")]
    [InlineData("""{"userId":"1","displayName":"Max","username":"max","photoUrl":null,"relation":"Friend"}""", "$.relation:")]
    [InlineData("""{"userId":"1","displayName":"Max","username":"max","photoUrl":null,"relation":"FRIEND","email":"x"}""", "$.email: not declared")]
    [InlineData("""{"userId":1,"displayName":"Max","username":"max","photoUrl":null,"relation":"FRIEND"}""", "$.userId: expected string")]
    [InlineData("""{"user_id":"1","displayName":"Max","username":"max","photoUrl":null,"relation":"FRIEND"}""", "$.userId: missing")]
    public void RejectsDrift(string json, string expectedError)
    {
        var exception = Assert.Throws<TrueException>(() => SpaContract.Verify(Json(json), "PersonDto"));

        Assert.Contains(expectedError, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void HandlesNullableInlineAndLiteralTypes()
    {
        var settings = """
            {"theme":"dark","timeFormat":"24h","weekStartsOn":1,"defaultEventDurationMinutes":120,
             "usualFreeTime":{"weekdays":{"start":"18:00","end":"23:00"},"weekends":null},
             "notifications":{"invitations":true,"deadlineReminders":true,"eventConfirmed":true,
                              "eventCancelled":true,"eventReminder":false,"eventReminderHours":24}}
            """;

        SpaContract.Verify(Json(settings), "UserSettingsDto");
        Assert.Throws<TrueException>(() =>
            SpaContract.Verify(Json(settings.Replace("\"weekStartsOn\":1", "\"weekStartsOn\":3", StringComparison.Ordinal)), "UserSettingsDto"));
    }
}
