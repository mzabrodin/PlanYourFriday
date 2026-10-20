namespace PlanYourFriday.Domain.Common;

public sealed class EntityNotFoundException(string code, string message) : DomainException(code, message);
