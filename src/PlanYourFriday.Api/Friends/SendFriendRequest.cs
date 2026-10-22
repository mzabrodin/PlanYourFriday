using System.ComponentModel.DataAnnotations;

namespace PlanYourFriday.Api.Friends;

public sealed record SendFriendRequest([Required] Guid? UserId);
