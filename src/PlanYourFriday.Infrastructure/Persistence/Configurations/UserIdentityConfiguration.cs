using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Infrastructure.Persistence.Configurations;

internal sealed class UserIdentityConfiguration : IEntityTypeConfiguration<UserIdentity>
{
    public void Configure(EntityTypeBuilder<UserIdentity> builder)
    {
        builder.ToTable("user_identities");

        builder.HasIndex(i => new { i.Provider, i.Subject }).IsUnique();
    }
}
