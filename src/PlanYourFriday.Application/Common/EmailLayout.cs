using System.Globalization;
using System.Net;
using System.Text;

namespace PlanYourFriday.Application.Common;

/// <summary>
/// Renders <see cref="EmailContent"/> in the PlanYourFriday look. The HTML is table-based with inline styles,
/// because that is what email clients reliably support.
/// </summary>
public static class EmailLayout
{
    private const string Font = "'Inter',-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif";
    private const string DisplayFont = "'Bricolage Grotesque','Inter',-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif";
    private const string Primary = "#e2572b";
    private const string Text = "#1d1b18";
    private const string Muted = "#6b665e";
    private const string Border = "#e3e0d9";
    private const string Background = "#f6f5f2";

    public static EmailMessage Render(EmailContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return new EmailMessage(content.Subject, RenderText(content), RenderHtml(content));
    }

    private static string RenderText(EmailContent content)
    {
        var text = new StringBuilder();
        text.Append(content.Heading).Append("\n\n");
        foreach (var paragraph in content.Paragraphs)
        {
            text.Append(paragraph).Append("\n\n");
        }

        if (content.Details.Count > 0)
        {
            foreach (var detail in content.Details)
            {
                text.Append(detail.Label).Append(": ").Append(detail.Value).Append('\n');
            }

            text.Append('\n');
        }

        text.Append(content.ActionLabel).Append(": ").Append(content.ActionUrl).Append("\n\n");
        text.Append("--\n").Append(content.Footer);
        if (content.SettingsUrl is not null)
        {
            text.Append("\nEmail settings: ").Append(content.SettingsUrl);
        }

        return text.ToString();
    }

    private static string RenderHtml(EmailContent content)
    {
        var (accent, tint) = Colors(content.Tone);
        var paragraphs = new StringBuilder();
        foreach (var paragraph in content.Paragraphs)
        {
            paragraphs.Append(
                CultureInfo.InvariantCulture,
                $$"""<p style="margin:0 0 14px;font-size:16px;line-height:1.6;color:#4a463f;">{{E(paragraph)}}</p>""");
        }

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="color-scheme" content="light only">
            <title>{{E(content.Subject)}}</title>
            <style>
              @media (max-width: 620px) {
                .pyf-card { padding: 28px 22px !important; }
                .pyf-heading { font-size: 24px !important; }
              }
            </style>
            </head>
            <body style="margin:0;padding:0;background:{{Background}};-webkit-font-smoothing:antialiased;">
            <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">{{E(content.Paragraphs.Count > 0 ? content.Paragraphs[0] : content.Heading)}}</div>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:{{Background}};">
              <tr>
                <td align="center" style="padding:32px 12px;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;">
                    <tr>
                      <td style="padding:0 4px 20px;font-family:{{DisplayFont}};font-size:20px;font-weight:800;letter-spacing:-0.4px;color:{{Text}};">
                        Plan<span style="color:{{Primary}};">Your</span>Friday
                      </td>
                    </tr>
                    <tr>
                      <td style="background:#ffffff;border:1px solid {{Border}};border-top:4px solid {{accent}};border-radius:16px;">
                        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
                          <tr>
                            <td class="pyf-card" style="padding:36px 40px;font-family:{{Font}};color:{{Text}};">
                              <span style="display:inline-block;padding:5px 12px;border-radius:999px;background:{{tint}};color:{{accent}};font-size:12px;font-weight:700;letter-spacing:0.6px;text-transform:uppercase;">{{E(content.Eyebrow)}}</span>
                              <h1 class="pyf-heading" style="margin:18px 0 16px;font-family:{{DisplayFont}};font-size:28px;line-height:1.2;font-weight:700;letter-spacing:-0.5px;color:{{Text}};">{{E(content.Heading)}}</h1>
                              {{paragraphs}}
                              {{Details(content.Details, accent)}}
                              <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:26px 0 8px;">
                                <tr>
                                  <td style="border-radius:999px;background:{{Primary}};">
                                    <a href="{{E(content.ActionUrl.ToString())}}" style="display:inline-block;padding:14px 28px;font-family:{{Font}};font-size:16px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:999px;">{{E(content.ActionLabel)}} &rarr;</a>
                                  </td>
                                </tr>
                              </table>
                              <p style="margin:16px 0 0;font-size:13px;line-height:1.5;color:{{Muted}};">Button not working? Paste this link into your browser:<br><a href="{{E(content.ActionUrl.ToString())}}" style="color:{{Muted}};word-break:break-all;">{{E(content.ActionUrl.ToString())}}</a></p>
                            </td>
                          </tr>
                        </table>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:22px 8px 0;font-family:{{Font}};font-size:12px;line-height:1.6;color:{{Muted}};text-align:center;">
                        {{E(content.Footer)}}{{SettingsLink(content.SettingsUrl)}}<br>
                        PlanYourFriday &middot; find a time that works for everyone
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
            </body>
            </html>
            """;
    }

    private static string Details(IReadOnlyList<EmailDetail> details, string accent)
    {
        if (details.Count == 0)
        {
            return string.Empty;
        }

        var rows = new StringBuilder();
        foreach (var detail in details)
        {
            var value = Uri.TryCreate(detail.Value, UriKind.Absolute, out var url) && url.Scheme is "http" or "https"
                ? $$"""<a href="{{E(detail.Value)}}" style="color:{{accent}};word-break:break-all;">{{E(detail.Value)}}</a>"""
                : E(detail.Value);
            rows.Append(
                CultureInfo.InvariantCulture,
                $$"""
                <tr>
                  <td valign="top" style="padding:6px 16px 6px 0;width:64px;font-size:12px;font-weight:600;letter-spacing:0.5px;text-transform:uppercase;color:{{Muted}};">{{E(detail.Label)}}</td>
                  <td valign="top" style="padding:6px 0;font-size:15px;line-height:1.5;color:{{Text}};">{{value}}</td>
                </tr>
                """);
        }

        return $$"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="margin:8px 0 0;background:{{Background}};border-left:3px solid {{accent}};border-radius:10px;">
              <tr>
                <td style="padding:14px 18px;font-family:{{Font}};">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">{{rows}}</table>
                </td>
              </tr>
            </table>
            """;
    }

    private static string SettingsLink(Uri? settingsUrl)
    {
        return settingsUrl is null
            ? string.Empty
            : $$""" <a href="{{E(settingsUrl.ToString())}}" style="color:{{Muted}};">Email settings</a>""";
    }

    private static (string Accent, string Tint) Colors(EmailTone tone)
    {
        return tone switch
        {
            EmailTone.Planning => ("#2e6fd1", "#e3ecfb"),
            EmailTone.Scheduled => ("#1f8a5b", "#e2f4ea"),
            EmailTone.Cancelled => ("#8a8378", "#f0eee9"),
            _ => throw new ArgumentOutOfRangeException(nameof(tone), tone, null),
        };
    }

    private static string E(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
