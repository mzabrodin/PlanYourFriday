using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Infrastructure.Persistence.Configurations;

internal sealed class CalendarConnectionConfiguration : IEntityTypeConfiguration<CalendarConnection>
{
    public void Configure(EntityTypeBuilder<CalendarConnection> builder)
    {
        builder.ToTable("calendar_connections");

        builder.HasIndex(c => new { c.UserId, c.Provider, c.ExternalAccountId }).IsUnique();
    }
}
