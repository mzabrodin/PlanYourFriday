using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(u => u.Email).HasColumnType("citext");
        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasMany(u => u.Identities)
            .WithOne()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.CalendarConnections)
            .WithOne()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
