using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections;

public sealed class ProjectionsDbContext(DbContextOptions<ProjectionsDbContext> options) : DbContext(options)
{
    public DbSet<AccountReadModel> Accounts => Set<AccountReadModel>();
    public DbSet<ExpenseReadModel> Expenses => Set<ExpenseReadModel>();
    public DbSet<BudgetReadModel> Budgets => Set<BudgetReadModel>();
    public DbSet<BudgetCategoryLimitReadModel> BudgetCategoryLimits => Set<BudgetCategoryLimitReadModel>();
    public DbSet<SpendingSummaryReadModel> SpendingSummaries => Set<SpendingSummaryReadModel>();
    public DbSet<MonthlyStatsReadModel> MonthlyStats => Set<MonthlyStatsReadModel>();
    public DbSet<ProjectionCheckpoint> Checkpoints => Set<ProjectionCheckpoint>();
    public DbSet<ProcessedMessageReadModel> ProcessedMessages => Set<ProcessedMessageReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("projections");

        modelBuilder.Entity<AccountReadModel>(e =>
        {
            e.ToTable("accounts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
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

        modelBuilder.Entity<BudgetReadModel>(e =>
        {
            e.ToTable("budgets");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.Month, x.Year }).IsUnique();
        });

        modelBuilder.Entity<BudgetCategoryLimitReadModel>(e =>
        {
            e.ToTable("budget_category_limits");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.BudgetId);
            e.HasIndex(x => new { x.BudgetId, x.CategoryId }).IsUnique();
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
