using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PlanYourFriday.Domain.Groups;

namespace PlanYourFriday.Infrastructure.Persistence.Configurations;

internal sealed class TimeOptionConfiguration : IEntityTypeConfiguration<TimeOption>
{
    public void Configure(EntityTypeBuilder<TimeOption> builder)
    {
        builder.ToTable("time_options", t => t.HasCheckConstraint("ck_time_options_time", "ends_at > starts_at"));

        builder.ComplexProperty(o => o.Time, time =>
        {
            time.Property(t => t.Start).HasColumnName("starts_at");
            time.Property(t => t.End).HasColumnName("ends_at");
        });

        builder.HasMany(o => o.Votes)
            .WithOne()
            .HasForeignKey(v => v.OptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
