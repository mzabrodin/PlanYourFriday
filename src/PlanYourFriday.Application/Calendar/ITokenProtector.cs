namespace PlanYourFriday.Application.Calendar;

public interface ITokenProtector
{
    string Protect(string token);

    string Unprotect(string protectedToken);
}
