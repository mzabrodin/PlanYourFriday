namespace PlanYourFriday.Domain.Common;

public abstract class Entity : AuditableEntity
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
}
