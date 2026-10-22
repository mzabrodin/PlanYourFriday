using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        builder.Property(u => u.Username).HasColumnType("citext").HasMaxLength(Usernames.MaxLength);
        builder.HasIndex(u => u.Username).IsUnique();

        builder.HasIndex(u => u.FriendCode).IsUnique();

        builder.Property(u => u.Settings)
            .HasColumnType("jsonb")
            .HasConversion(
                settings => JsonSerializer.Serialize(settings, JsonSerializerOptions.Web),
                json => JsonSerializer.Deserialize<UserSettings>(json, JsonSerializerOptions.Web) ?? UserSettings.Default,
                new ValueComparer<UserSettings>((a, b) => Equals(a, b), s => s.GetHashCode(), s => s));

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
