using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Infrastructure.Persistence.Configurations;

internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events", t => t.HasCheckConstraint("ck_events_time", "ends_at > starts_at"));

        builder.ComplexProperty(e => e.Time, time =>
        {
            time.Property(t => t.Start).HasColumnName("starts_at");
            time.Property(t => t.End).HasColumnName("ends_at");
        });

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.GroupId).IsUnique().HasFilter("status = 'confirmed'");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.CalendarLinks)
            .WithOne()
            .HasForeignKey(l => l.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
