using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Infrastructure.Persistence.Configurations;

internal sealed class OptionVoteConfiguration : IEntityTypeConfiguration<OptionVote>
{
    public void Configure(EntityTypeBuilder<OptionVote> builder)
    {
        builder.ToTable("time_option_votes");

        builder.HasKey(v => new { v.OptionId, v.UserId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
