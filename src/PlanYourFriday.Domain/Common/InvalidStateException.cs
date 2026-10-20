namespace PlanYourFriday.Domain.Common;

public sealed class InvalidStateException(string code, string message) : DomainException(code, message);
