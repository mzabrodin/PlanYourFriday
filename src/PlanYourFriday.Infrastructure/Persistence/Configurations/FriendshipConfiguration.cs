using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PlanYourFriday.Domain.Friends;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Infrastructure.Persistence.Configurations;

internal sealed class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("friendships", t => t.HasCheckConstraint("ck_friendships_order", "user_low_id < user_high_id"));

        builder.Ignore(f => f.AddresseeId);
        builder.Ignore(f => f.IsAccepted);

        builder.HasIndex(f => new { f.UserLowId, f.UserHighId }).IsUnique();
        builder.HasIndex(f => f.UserHighId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.UserLowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.UserHighId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
