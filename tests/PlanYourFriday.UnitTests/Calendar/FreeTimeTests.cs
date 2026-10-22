using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Domain.Users;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Calendar;

public class FreeTimeTests
{
    private static readonly UsualFreeTime Evenings = new(new DailyHours("18:00", "23:00"), null);

    [Fact]
    public void UsualSlots_UsesUsualHoursInUsersTimeZone()
    {
        var kyiv = TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");

        var slots = FreeTime.UsualSlots(Range(2, 0, 23), Evenings, kyiv);

        var slot = Assert.Single(slots);
        Assert.Equal(new DateTime(2026, 11, 2, 16, 0, 0, DateTimeKind.Utc), slot.Start);
        Assert.Equal(new DateTime(2026, 11, 2, 21, 0, 0, DateTimeKind.Utc), slot.End);
    }

    [Fact]
    public void UsualSlots_ReturnsWholeWindowWhenNoUsualHours()
    {
        var window = Range(2, 9, 18);

        Assert.Equal([window], FreeTime.UsualSlots(window, new UsualFreeTime(null, null), TimeZoneInfo.Utc));
    }

    [Fact]
    public void UsualSlots_HandlesHoursPastMidnight()
    {
        var lateNights = new UsualFreeTime(new DailyHours("22:00", "02:00"), new DailyHours("22:00", "02:00"));

        var slots = FreeTime.UsualSlots(Range(2, 0, 23), lateNights, TimeZoneInfo.Utc);

        Assert.Equal([Range(2, 0, 2), Range(2, 22, 23)], slots);
    }

    [Fact]
    public void Subtract_CutsBusyBlocksOutOfSlots()
    {
        var free = FreeTime.Subtract([Range(2, 10, 20)], [Range(2, 12, 13), Range(2, 12, 14), Range(2, 19, 22)]);

        Assert.Equal([Range(2, 10, 12), Range(2, 14, 19)], free);
    }
}
