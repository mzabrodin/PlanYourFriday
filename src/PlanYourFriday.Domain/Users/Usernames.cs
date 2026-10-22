using System.Text;

namespace PlanYourFriday.Domain.Users;

public static class Usernames
{
    public const int MinLength = 3;
    public const int MaxLength = 30;
    public const string Pattern = "^[a-z0-9_.]{3,30}$";

    public static string FromEmail(string email)
    {
        var local = email.Split('@')[0].ToLowerInvariant();
        var builder = new StringBuilder();
        foreach (var c in local)
        {
            if (c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '.')
            {
                builder.Append(c);
            }
        }

        var candidate = builder.ToString().Trim('.');
        if (candidate.Length > MaxLength - 4)
        {
            candidate = candidate[..(MaxLength - 4)];
        }

        return candidate.Length < MinLength ? $"user{candidate}" : candidate;
    }
}
