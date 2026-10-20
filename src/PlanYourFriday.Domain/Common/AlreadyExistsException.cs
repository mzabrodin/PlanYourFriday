namespace PlanYourFriday.Domain.Common;

public sealed class AlreadyExistsException(string code, string message) : DomainException(code, message);
