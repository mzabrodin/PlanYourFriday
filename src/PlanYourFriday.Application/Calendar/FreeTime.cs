using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Calendar;

public static class FreeTime
{
    public static IReadOnlyList<TimeRange> UsualSlots(TimeRange window, UsualFreeTime usual, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(usual);

        if (usual.Weekdays is null && usual.Weekends is null)
        {
            return [window];
        }

        var slots = new List<TimeRange>();
        var firstDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(window.Start, timeZone)).AddDays(-1);
        var lastDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(window.End, timeZone));
        for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            var weekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var hours = weekend ? usual.Weekends : usual.Weekdays;
            if (hours is null)
            {
                continue;
            }

            var start = ToUtc(day, TimeOnly.Parse(hours.Start, System.Globalization.CultureInfo.InvariantCulture), timeZone);
            var end = ToUtc(day, TimeOnly.Parse(hours.End, System.Globalization.CultureInfo.InvariantCulture), timeZone);
            if (end <= start)
            {
                end = end.AddDays(1);
            }

            var slot = new TimeRange(start, end).Intersect(window);
            if (slot is not null)
            {
                slots.Add(slot);
            }
        }

        return slots;
    }

    public static IReadOnlyList<TimeRange> Subtract(IEnumerable<TimeRange> slots, IEnumerable<TimeRange> busy)
    {
        var busyRanges = busy.OrderBy(b => b.Start).ToList();
        var free = new List<TimeRange>();
        foreach (var slot in slots.OrderBy(s => s.Start))
        {
            var start = slot.Start;
            foreach (var block in busyRanges.Where(b => b.Overlaps(slot)))
            {
                if (block.Start > start)
                {
                    free.Add(new TimeRange(start, block.Start));
                }

                if (block.End > start)
                {
                    start = block.End;
                }
            }

            if (start < slot.End)
            {
                free.Add(new TimeRange(start, slot.End));
            }
        }

        return free;
    }

    private static DateTime ToUtc(DateOnly day, TimeOnly time, TimeZoneInfo timeZone)
    {
        var local = day.ToDateTime(time, DateTimeKind.Unspecified);
        while (timeZone.IsInvalidTime(local))
        {
            local = local.AddMinutes(30);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }
}
