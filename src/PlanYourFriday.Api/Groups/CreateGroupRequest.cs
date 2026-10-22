using System.ComponentModel.DataAnnotations;

using PlanYourFriday.Api.Common;

namespace PlanYourFriday.Api.Groups;

public sealed record CreateGroupRequest(
    [Required]
    [MaxLength(200)]
    string Title,
    [MaxLength(2000)]
    string? Description,
    [Required]
    TimeRangeRequest Window,
    [Range(15, 7 * 24 * 60)]
    int EventDurationMinutes,
    DateTimeOffset? AvailabilityDeadline,
    [MaxLength(50)]
    IReadOnlyList<Guid>? MemberIds,
    Guid? PollId);
