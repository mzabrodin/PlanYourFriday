namespace PlanYourFriday.Application.Common;

public sealed class UnauthenticatedException(string message = "Authentication is required.") : Exception(message);
