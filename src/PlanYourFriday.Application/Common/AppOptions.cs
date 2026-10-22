namespace PlanYourFriday.Application.Common;

public sealed class AppOptions
{
    public const string Section = "App";

    public Uri PublicUrl { get; set; } = new("http://localhost:5173");
}
