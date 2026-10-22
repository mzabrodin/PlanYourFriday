using System.ComponentModel.DataAnnotations;

using PlanYourFriday.Api.Common;

namespace PlanYourFriday.Api.Groups;

public sealed record StartVotingRequest(
    [Required]
    [Length(1, 6)]
    IReadOnlyList<TimeRangeRequest> Options,
    DateTimeOffset? Deadline);
