using PlanYourFriday.Domain.Common;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Domain;

public class TimeRangeTests
{
    [Fact]
    public void Constructor_RejectsEndNotAfterStart()
    {
        var empty = Assert.Throws<BusinessRuleViolationException>(() => new TimeRange(At(1, 10), At(1, 10)));
        var reversed = Assert.Throws<BusinessRuleViolationException>(() => new TimeRange(At(1, 10), At(1, 9)));

        Assert.Equal("time_range.end_before_start", empty.Code);
        Assert.Equal("time_range.end_before_start", reversed.Code);
    }

    [Fact]
    public void Constructor_RejectsNonUtcTimes()
    {
        var local = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Unspecified);

        var localStart = Assert.Throws<BusinessRuleViolationException>(() => new TimeRange(local, At(1, 12)));
        var unspecifiedEnd = Assert.Throws<BusinessRuleViolationException>(() => new TimeRange(At(1, 10), unspecified));

        Assert.Equal("time_range.not_utc", localStart.Code);
        Assert.Equal("time_range.not_utc", unspecifiedEnd.Code);
    }

    [Theory]
    [InlineData(9, 12, 11, 14, true)]
    [InlineData(9, 12, 10, 11, true)]
    [InlineData(9, 12, 12, 14, false)]
    [InlineData(9, 12, 6, 9, false)]
    public void Overlaps_TreatsTouchingRangesAsNotOverlapping(int aStart, int aEnd, int bStart, int bEnd, bool expected)
    {
        var a = Range(1, aStart, aEnd);
        var b = Range(1, bStart, bEnd);

        Assert.Equal(expected, a.Overlaps(b));
        Assert.Equal(expected, b.Overlaps(a));
    }

    [Fact]
    public void Intersect_ReturnsCommonPart()
    {
        Assert.Equal(Range(1, 11, 12), Range(1, 9, 12).Intersect(Range(1, 11, 14)));
    }

    [Fact]
    public void Intersect_ReturnsNullWhenRangesDoNotOverlap()
    {
        Assert.Null(Range(1, 9, 12).Intersect(Range(1, 12, 14)));
    }

    [Fact]
    public void Contains_IncludesBoundaries()
    {
        var window = Range(1, 9, 18);

        Assert.True(window.Contains(Range(1, 9, 18)));
        Assert.True(window.Contains(Range(1, 10, 11)));
        Assert.False(window.Contains(Range(1, 8, 10)));
    }
}
