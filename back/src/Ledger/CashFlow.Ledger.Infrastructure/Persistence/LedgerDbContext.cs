using Microsoft.EntityFrameworkCore;

namespace CashFlow.Ledger.Infrastructure.Persistence;

internal sealed class LedgerDbContext(DbContextOptions<LedgerDbContext> options) : DbContext(options)
{
    public DbSet<EntryRecord> Entries => Set<EntryRecord>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        var entry = builder.Entity<EntryRecord>();
        entry.ToTable("entry");
        entry.HasKey(e => e.Id);
        entry.Property(e => e.Id).HasColumnName("id");
        entry.Property(e => e.Type).HasColumnName("type");
        entry.Property(e => e.Amount).HasColumnName("amount").HasPrecision(19, 2);
        entry.Property(e => e.CompetenceDate).HasColumnName("competence_date");
        entry.Property(e => e.Description).HasColumnName("description").HasMaxLength(200);
        entry.Property(e => e.RecordedAt).HasColumnName("recorded_at");

        var outbox = builder.Entity<OutboxMessage>();
        outbox.ToTable("outbox_message");
        outbox.HasKey(m => m.Id);
        outbox.Property(m => m.Id).HasColumnName("id");
        outbox.Property(m => m.EntryId).HasColumnName("entry_id");
        outbox.Property(m => m.EventType).HasColumnName("event_type").HasMaxLength(100);
        outbox.Property(m => m.Payload).HasColumnName("payload");
        outbox.Property(m => m.CreatedAt).HasColumnName("created_at");
        outbox.Property(m => m.PublishedAt).HasColumnName("published_at");
        outbox.Property(m => m.ClaimedBy).HasColumnName("claimed_by").HasMaxLength(64);
        outbox.Property(m => m.ClaimedAt).HasColumnName("claimed_at");
        outbox.Property(m => m.AttemptCount).HasColumnName("attempt_count");
    }
}
