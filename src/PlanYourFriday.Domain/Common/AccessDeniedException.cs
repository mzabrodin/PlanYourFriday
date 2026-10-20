namespace PlanYourFriday.Domain.Common;

public sealed class AccessDeniedException(string code, string message) : DomainException(code, message);
