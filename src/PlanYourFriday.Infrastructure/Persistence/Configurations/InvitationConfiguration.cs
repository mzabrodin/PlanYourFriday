using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Infrastructure.Persistence.Configurations;

internal sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("invitations");

        builder.Property(i => i.Email).HasColumnType("citext");
        builder.HasIndex(i => new { i.GroupId, i.Email }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.AcceptedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
