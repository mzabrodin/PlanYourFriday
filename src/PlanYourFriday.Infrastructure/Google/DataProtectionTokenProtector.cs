using Microsoft.AspNetCore.DataProtection;

using PlanYourFriday.Application.Calendar;

namespace PlanYourFriday.Infrastructure.Google;

internal sealed class DataProtectionTokenProtector(IDataProtectionProvider provider) : ITokenProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("PlanYourFriday.CalendarTokens");

    public string Protect(string token)
    {
        return _protector.Protect(token);
    }

    public string Unprotect(string protectedToken)
    {
        return _protector.Unprotect(protectedToken);
    }
}
