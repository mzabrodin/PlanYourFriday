namespace PlanYourFriday.Domain.Common;

public sealed record TimeRange
{
    public TimeRange(DateTime start, DateTime end)
    {
        if (start.Kind != DateTimeKind.Utc || end.Kind != DateTimeKind.Utc)
        {
            throw new BusinessRuleViolationException("time_range.not_utc", "Time range bounds must be in UTC.");
        }

        if (end <= start)
        {
            throw new BusinessRuleViolationException("time_range.end_before_start", "Time range end must be after its start.");
        }

        Start = start;
        End = end;
    }

    public DateTime Start { get; }

    public DateTime End { get; }

    public TimeSpan Duration => End - Start;

    public bool Overlaps(TimeRange other)
    {
        return Start < other.End && other.Start < End;
    }

    public bool Contains(TimeRange other)
    {
        return Start <= other.Start && other.End <= End;
    }

    public TimeRange? Intersect(TimeRange other)
    {
        if (!Overlaps(other))
        {
            return null;
        }

        var start = Start > other.Start ? Start : other.Start;
        var end = End < other.End ? End : other.End;
        return new TimeRange(start, end);
    }
}
