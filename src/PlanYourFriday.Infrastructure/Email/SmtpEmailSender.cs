using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Options;

using MimeKit;

using PlanYourFriday.Application.Common;

namespace PlanYourFriday.Infrastructure.Email;

internal sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private const int ImplicitTlsPort = 465;

    public async Task SendAsync(string to, EmailMessage email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);

        var settings = options.Value;
        using var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(settings.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = email.Subject;
        message.Body = new BodyBuilder { TextBody = email.Text, HtmlBody = email.Html }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.Host, settings.Port, Security(settings), cancellationToken);
        if (!string.IsNullOrEmpty(settings.UserName))
        {
            await client.AuthenticateAsync(settings.UserName, settings.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static SecureSocketOptions Security(SmtpOptions settings)
    {
        if (!settings.EnableSsl)
        {
            return SecureSocketOptions.None;
        }

        return settings.Port == ImplicitTlsPort ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
    }
}

internal sealed class SmtpOptions
{
    public const string Section = "Smtp";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public bool EnableSsl { get; set; }

    public string From { get; set; } = "PlanYourFriday <no-reply@planyourfriday.local>";

    public string? UserName { get; set; }

    public string? Password { get; set; }
}
