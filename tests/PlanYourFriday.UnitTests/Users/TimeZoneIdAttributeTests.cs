using PlanYourFriday.Api.Users;

namespace PlanYourFriday.UnitTests.Users;

public class TimeZoneIdAttributeTests
{
    [Theory]
    [InlineData("Europe/Kyiv", true)]
    [InlineData("America/New_York", true)]
    [InlineData("UTC", true)]
    [InlineData("FLE Standard Time", false)]
    [InlineData("Mars/Olympus_Mons", false)]
    public void IsValid_AcceptsOnlyIanaTimeZones(string timeZone, bool expected)
    {
        Assert.Equal(expected, new TimeZoneIdAttribute().IsValid(timeZone));
    }
}
