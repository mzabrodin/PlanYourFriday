using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Groups;

public sealed class GroupMember : AuditableEntity
{
    private readonly List<AvailabilitySlot> _availability = [];

    private GroupMember()
    {
    }

    internal GroupMember(Guid groupId, Guid userId, MemberRole role)
    {
        GroupId = groupId;
        UserId = userId;
        Role = role;
    }

    public Guid GroupId { get; private set; }

    public Guid UserId { get; private set; }

    public MemberRole Role { get; private set; }

    public IReadOnlyCollection<AvailabilitySlot> Availability => _availability;

    internal void SetAvailability(IEnumerable<TimeRange> slots)
    {
        _availability.Clear();
        _availability.AddRange(slots.Select(slot => new AvailabilitySlot(slot)));
    }
}
