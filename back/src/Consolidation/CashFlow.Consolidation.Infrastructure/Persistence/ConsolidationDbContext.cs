using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infrastructure.Persistence;

internal sealed class ConsolidationDbContext(DbContextOptions<ConsolidationDbContext> options) : DbContext(options)
{
    public DbSet<DailyBalanceRecord> DailyBalances => Set<DailyBalanceRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        var balance = builder.Entity<DailyBalanceRecord>();
        balance.ToTable("daily_balance");
        balance.HasKey(b => b.CompetenceDate);
        balance.Property(b => b.CompetenceDate).HasColumnName("competence_date");
        balance.Property(b => b.TotalCredits).HasColumnName("total_credits").HasPrecision(19, 2);
        balance.Property(b => b.TotalDebits).HasColumnName("total_debits").HasPrecision(19, 2);
        balance.Property(b => b.EntryCount).HasColumnName("entry_count");
        balance.Property(b => b.UpdatedAt).HasColumnName("updated_at");
    }
}
