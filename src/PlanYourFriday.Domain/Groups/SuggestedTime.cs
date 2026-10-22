using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Groups;

public sealed record SuggestedTime(TimeRange Time, IReadOnlyList<Guid> FreeUserIds);
