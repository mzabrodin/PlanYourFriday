using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Infrastructure.Persistence.Configurations;

internal sealed class CalendarEventLinkConfiguration : IEntityTypeConfiguration<CalendarEventLink>
{
    public void Configure(EntityTypeBuilder<CalendarEventLink> builder)
    {
        builder.ToTable("calendar_event_links");

        builder.HasIndex(l => new { l.EventId, l.ConnectionId }).IsUnique();

        builder.HasOne<CalendarConnection>()
            .WithMany()
            .HasForeignKey(l => l.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
