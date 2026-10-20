using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.UnitTests.Domain;

internal static class TestTime
{
    public static DateTime At(int day, int hour)
    {
        return new DateTime(2026, 11, day, hour, 0, 0, DateTimeKind.Utc);
    }

    public static TimeRange Range(int day, int startHour, int endHour)
    {
        return new TimeRange(At(day, startHour), At(day, endHour));
    }
}
