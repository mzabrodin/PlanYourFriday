using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Npgsql;

using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Friends;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Polls;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider timeProvider)
    : DbContext(options), IUnitOfWork, IDataProtectionKeyContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();

    public DbSet<CalendarConnection> CalendarConnections => Set<CalendarConnection>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<TimeOption> TimeOptions => Set<TimeOption>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<CalendarEventLink> CalendarEventLinks => Set<CalendarEventLink>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<Friendship> Friendships => Set<Friendship>();

    public DbSet<Poll> Polls => Set<Poll>();

    public DbSet<PollParticipant> PollParticipants => Set<PollParticipant>();

    public DbSet<PollOption> PollOptions => Set<PollOption>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new AlreadyExistsException(
                "resource.already_exists", "This was changed by someone else at the same time. Try again.");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(Entity).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType).Property(nameof(Entity.Id)).ValueGeneratedNever();
        }
    }

    private void StampAuditFields()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(e => e.CreatedAt).CurrentValue = now;
                    entry.Property(e => e.UpdatedAt).CurrentValue = now;
                    break;
                case EntityState.Modified:
                    entry.Property(e => e.UpdatedAt).CurrentValue = now;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                default:
                    break;
            }
        }
    }
}
