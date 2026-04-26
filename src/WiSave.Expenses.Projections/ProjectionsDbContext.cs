using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections;

public sealed class ProjectionsDbContext(DbContextOptions<ProjectionsDbContext> options) : DbContext(options)
{
    public DbSet<FundingAccountReadModel> FundingAccounts => Set<FundingAccountReadModel>();
    public DbSet<FundingPaymentInstrumentReadModel> FundingPaymentInstruments => Set<FundingPaymentInstrumentReadModel>();
    public DbSet<ExpenseReadModel> Expenses => Set<ExpenseReadModel>();
    public DbSet<SpendingSummaryReadModel> SpendingSummaries => Set<SpendingSummaryReadModel>();
    public DbSet<MonthlyStatsReadModel> MonthlyStats => Set<MonthlyStatsReadModel>();
    public DbSet<ProjectionCheckpoint> Checkpoints => Set<ProjectionCheckpoint>();
    public DbSet<ProcessedMessageReadModel> ProcessedMessages => Set<ProcessedMessageReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("projections");

        modelBuilder.Entity<FundingAccountReadModel>(e =>
        {
            e.ToTable("funding_accounts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Kind).HasMaxLength(32);
            e.Property(x => x.Currency).HasMaxLength(16);
            e.Property(x => x.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<FundingPaymentInstrumentReadModel>(e =>
        {
            e.ToTable("funding_payment_instruments");
            e.HasKey(x => new { x.FundingAccountId, x.Id });
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.FundingAccountId);
            e.Property(x => x.Kind).HasMaxLength(32);
            e.Property(x => x.LastFourDigits).HasMaxLength(4);
            e.Property(x => x.Network).HasMaxLength(32);
        });

        modelBuilder.Entity<ExpenseReadModel>(e =>
        {
            e.ToTable("expenses");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.AccountId);
            e.HasIndex(x => x.CategoryId);
            e.HasIndex(x => x.Date);
        });

        modelBuilder.Entity<SpendingSummaryReadModel>(e =>
        {
            e.ToTable("spending_summaries");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.Month, x.Year });
            e.HasIndex(x => new { x.UserId, x.Month, x.Year, x.CategoryId }).IsUnique();
        });

        modelBuilder.Entity<MonthlyStatsReadModel>(e =>
        {
            e.ToTable("monthly_stats");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.Year });
            e.HasIndex(x => new { x.UserId, x.Year, x.Month }).IsUnique();
        });

        modelBuilder.Entity<ProjectionCheckpoint>(e =>
        {
            e.ToTable("checkpoints");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ProcessedMessageReadModel>(e =>
        {
            e.ToTable("processed_messages");
            e.HasKey(x => x.MessageId);
        });
    }
}
