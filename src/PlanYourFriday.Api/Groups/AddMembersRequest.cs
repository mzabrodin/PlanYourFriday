using System.ComponentModel.DataAnnotations;

namespace PlanYourFriday.Api.Groups;

public sealed record AddMembersRequest(
    [Required]
    [Length(1, 50)]
    IReadOnlyList<Guid> UserIds);
