using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PlanYourFriday.Application.Outbox;

namespace PlanYourFriday.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.Property(m => m.Payload).HasColumnType("jsonb");

        builder.HasIndex(m => m.NextAttemptAt).HasFilter("processed_at IS NULL");
    }
}
