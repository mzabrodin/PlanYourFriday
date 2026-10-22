using System.ComponentModel.DataAnnotations;

using PlanYourFriday.Application.Events;

namespace PlanYourFriday.Api.Events;

public record EventDetailsRequest(
    [Required]
    [MaxLength(200)]
    string Title,
    [MaxLength(300)]
    string? Location,
    [Url]
    [MaxLength(500)]
    string? MeetingLink,
    [MaxLength(2000)]
    string? Description)
{
    public EventDetails ToDetails()
    {
        return new EventDetails(Title.Trim(), Clean(Description), Clean(Location), Clean(MeetingLink));
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
