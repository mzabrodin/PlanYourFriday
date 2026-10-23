namespace PlanYourFriday.Application.Common;

public interface IEmailSender
{
    Task SendAsync(string to, EmailMessage message, CancellationToken cancellationToken);
}
