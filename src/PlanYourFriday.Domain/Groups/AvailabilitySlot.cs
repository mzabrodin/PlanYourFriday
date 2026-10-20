using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Groups;

public sealed class AvailabilitySlot
{
    private AvailabilitySlot()
    {
    }

    internal AvailabilitySlot(TimeRange slot)
    {
        Slot = slot;
    }

    public long Id { get; private set; }

    public TimeRange Slot { get; private set; } = null!;
}
