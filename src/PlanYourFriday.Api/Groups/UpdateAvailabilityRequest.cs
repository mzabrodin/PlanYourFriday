using System.ComponentModel.DataAnnotations;

using PlanYourFriday.Api.Common;

namespace PlanYourFriday.Api.Groups;

public sealed record UpdateAvailabilityRequest(
    [Required]
    [MaxLength(200)]
    IReadOnlyList<TimeRangeRequest> Slots);
