using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Infrastructure.Persistence.Configurations;

internal sealed class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.ToTable("group_members");

        builder.HasKey(m => new { m.GroupId, m.UserId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Availability)
            .WithOne()
            .HasForeignKey("GroupId", "UserId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
