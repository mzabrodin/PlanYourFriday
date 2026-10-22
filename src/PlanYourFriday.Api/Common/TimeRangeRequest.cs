using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Api.Common;

public sealed record TimeRangeRequest(DateTimeOffset Start, DateTimeOffset End)
{
    public TimeRange ToTimeRange()
    {
        return new TimeRange(Start.UtcDateTime, End.UtcDateTime);
    }
}
