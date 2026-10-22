namespace PlanYourFriday.Application.Events;

public sealed record EventDetails(string Title, string? Description, string? Location, string? MeetingLink);
