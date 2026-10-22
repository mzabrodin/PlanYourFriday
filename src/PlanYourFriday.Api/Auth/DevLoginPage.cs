using System.Net;

namespace PlanYourFriday.Api.Auth;

internal static class DevLoginPage
{
    public static string Render(string action, string returnUrl, string? timeZone)
    {
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Dev sign-in · Plan Your Friday</title>
              <style>
                body { font: 16px/1.5 system-ui, sans-serif; background: #f6f5f2; color: #1d1b16; display: grid; place-items: center; min-height: 100vh; margin: 0; padding: 16px; box-sizing: border-box; }
                form { background: #fff; padding: 28px; border-radius: 16px; box-shadow: 0 8px 30px rgb(0 0 0 / 8%); width: 100%; max-width: 360px; display: grid; gap: 14px; }
                h1 { font-size: 20px; margin: 0; }
                p { margin: 0; color: #6b665c; font-size: 14px; }
                label { display: grid; gap: 4px; font-size: 14px; font-weight: 600; }
                input { font: inherit; padding: 10px 12px; border: 1px solid #d8d3c8; border-radius: 10px; }
                button { font: inherit; font-weight: 600; padding: 11px; border: 0; border-radius: 10px; background: #2f5bea; color: #fff; cursor: pointer; }
              </style>
            </head>
            <body>
              <form method="post" action="{{WebUtility.HtmlEncode(action)}}">
                <h1>Dev sign-in</h1>
                <p>Google isn't configured, so in Development you can sign in as anyone. Use different emails to test as several friends.</p>
                <label>Email <input name="email" type="email" required autofocus placeholder="max@example.com"></label>
                <label>Name <input name="name" placeholder="Max"></label>
                <input type="hidden" name="returnUrl" value="{{WebUtility.HtmlEncode(returnUrl)}}">
                <input type="hidden" name="tz" value="{{WebUtility.HtmlEncode(timeZone ?? string.Empty)}}">
                <button type="submit">Sign in</button>
              </form>
            </body>
            </html>
            """;
    }
}
