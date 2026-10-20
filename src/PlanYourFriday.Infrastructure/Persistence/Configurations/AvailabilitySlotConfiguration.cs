using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PlanYourFriday.Domain.Groups;

namespace PlanYourFriday.Infrastructure.Persistence.Configurations;

internal sealed class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
{
    public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
    {
        builder.ToTable(
            "member_availability",
            t => t.HasCheckConstraint("ck_member_availability_slot", "slot_end > slot_start"));

        builder.ComplexProperty(s => s.Slot, slot =>
        {
            slot.Property(r => r.Start).HasColumnName("slot_start");
            slot.Property(r => r.End).HasColumnName("slot_end");
        });
    }
}
