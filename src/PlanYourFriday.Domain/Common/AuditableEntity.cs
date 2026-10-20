namespace PlanYourFriday.Domain.Common;

public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }
}
