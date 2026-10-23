namespace PlanYourFriday.Application.Common;

/// <summary>A ready-to-send email: the same content as plain text and as HTML.</summary>
public sealed record EmailMessage(string Subject, string Text, string Html);

/// <summary>Accent of the email, matching the stage colors of the SPA.</summary>
public enum EmailTone
{
    Planning,
    Scheduled,
    Cancelled,
}

public sealed record EmailDetail(string Label, string Value);

public sealed record EmailContent
{
    public required string Subject { get; init; }

    public required EmailTone Tone { get; init; }

    public required string Eyebrow { get; init; }

    public required string Heading { get; init; }

    public required IReadOnlyList<string> Paragraphs { get; init; }

    public IReadOnlyList<EmailDetail> Details { get; init; } = [];

    public required string ActionLabel { get; init; }

    public required Uri ActionUrl { get; init; }

    public required string Footer { get; init; }

    /// <summary>Link to the email preferences, shown in the footer of notification emails.</summary>
    public Uri? SettingsUrl { get; init; }
}
