using System.ComponentModel.DataAnnotations;

using PlanYourFriday.Api.Common;

namespace PlanYourFriday.Api.Events;

// Validation attributes live on the constructor parameters of the bound type, so the ones on
// EventDetailsRequest do not apply here and have to be repeated.
public sealed record ConfirmEventRequest(
    [Required]
    [MaxLength(200)]
    string Title,
    [Required] TimeRangeRequest Time,
    [MaxLength(300)]
    string? Location,
    [Url]
    [MaxLength(500)]
    string? MeetingLink,
    [MaxLength(2000)]
    string? Description)
    : EventDetailsRequest(Title, Location, MeetingLink, Description);
