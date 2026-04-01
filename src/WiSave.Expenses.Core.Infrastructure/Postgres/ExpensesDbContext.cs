using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Core.Infrastructure.Postgres.Entities;

namespace WiSave.Expenses.Core.Infrastructure.Postgres;

public sealed class ExpensesDbContext(DbContextOptions<ExpensesDbContext> options) : DbContext(options)
{
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<SubcategoryEntity> Subcategories => Set<SubcategoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CategoryEntity>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.UserId).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<SubcategoryEntity>(e =>
        {
            e.ToTable("subcategories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.CategoryId).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.HasIndex(x => x.CategoryId);
        });
    }
}
