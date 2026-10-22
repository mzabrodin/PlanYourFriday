using System.Text.Json;

using Microsoft.Extensions.Options;

using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Groups;

namespace PlanYourFriday.Application.Groups;

public sealed class InvitationEmailHandler(
    IGroupRepository groups,
    IUserRepository users,
    IEmailSender email,
    IOptions<AppOptions> options) : IOutboxHandler
{
    public string Type => InvitationEmailRequested.Type;

    public async Task HandleAsync(string payload, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<InvitationEmailRequested>(payload)
            ?? throw new JsonException("Empty invitation email payload.");
        var group = await groups.GetByIdAsync(request.GroupId, cancellationToken);
        var invitation = group?.Invitations.FirstOrDefault(i => i.Id == request.InvitationId);
        if (group is null || invitation?.Status is not (InvitationStatus.Pending or InvitationStatus.Sent))
        {
            return;
        }

        var organizer = await users.GetByIdAsync(group.OrganizerId, cancellationToken);
        var organizerName = organizer?.DisplayName ?? "A friend";
        var link = new Uri(options.Value.PublicUrl, $"/join/{Uri.EscapeDataString(group.InviteToken)}");

        await email.SendAsync(
            invitation.Email,
            $"{organizerName} invited you to \"{group.Title}\"",
            $"""
            Hi!

            {organizerName} is planning "{group.Title}" on Plan Your Friday and wants you to join.
            Mark when you're free and the app will find a time that works for everyone:

            {link}
            """,
            cancellationToken);

        group.MarkInvitationSent(invitation.Id);
    }
}
