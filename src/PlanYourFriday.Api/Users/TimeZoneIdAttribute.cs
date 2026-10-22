using System.ComponentModel.DataAnnotations;

namespace PlanYourFriday.Api.Users;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
internal sealed class TimeZoneIdAttribute() : ValidationAttribute("The {0} field must be an IANA time zone, e.g. Europe/Kyiv.")
{
    public override bool IsValid(object? value)
    {
        return value is not string id || TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out _);
    }
}
