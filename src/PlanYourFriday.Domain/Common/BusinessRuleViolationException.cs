namespace PlanYourFriday.Domain.Common;

public sealed class BusinessRuleViolationException(string code, string message) : DomainException(code, message);
