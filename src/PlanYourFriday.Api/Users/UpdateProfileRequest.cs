using System.ComponentModel.DataAnnotations;

using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Api.Users;

public sealed record UpdateProfileRequest(
    [Required]
    [MaxLength(100)]
    string DisplayName,
    [Required]
    [RegularExpression(Usernames.Pattern, ErrorMessage = "Username must be 3-30 characters: lowercase letters, digits, dots and underscores.")]
    string Username,
    [Required]
    [TimeZoneId]
    string TimeZone);
