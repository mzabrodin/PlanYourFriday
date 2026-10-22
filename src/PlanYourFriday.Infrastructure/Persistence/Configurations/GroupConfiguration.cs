using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Infrastructure.Persistence.Configurations;

internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups", t => t.HasCheckConstraint("ck_groups_window", "window_end > window_start"));

        builder.HasIndex(g => g.InviteToken).IsUnique();

        builder.ComplexProperty(g => g.Window, window =>
        {
            window.Property(w => w.Start).HasColumnName("window_start");
            window.Property(w => w.End).HasColumnName("window_end");
        });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(g => g.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(g => g.Members)
            .WithOne()
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Invitations)
            .WithOne()
            .HasForeignKey(i => i.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Options)
            .WithOne()
            .HasForeignKey(o => o.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
