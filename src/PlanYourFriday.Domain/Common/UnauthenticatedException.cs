namespace PlanYourFriday.Domain.Common;

public sealed class UnauthenticatedException(string code, string message) : DomainException(code, message);
