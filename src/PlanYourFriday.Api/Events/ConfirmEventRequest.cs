using System.ComponentModel.DataAnnotations;

using PlanYourFriday.Api.Common;

namespace PlanYourFriday.Api.Events;

public sealed record ConfirmEventRequest(
    string Title,
    [Required] TimeRangeRequest Time,
    string? Location,
    string? MeetingLink,
    string? Description)
    : EventDetailsRequest(Title, Location, MeetingLink, Description);
