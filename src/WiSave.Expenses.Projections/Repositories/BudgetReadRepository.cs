using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections.Repositories;

public sealed class BudgetReadRepository(ProjectionsDbContext db)
{
    public async Task<BudgetReadModel?> GetByMonthAsync(string userId, int month, int year, CancellationToken ct = default)
    {
        return await db.Budgets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.UserId == userId && b.Month == month && b.Year == year, ct);
    }

    public async Task<List<BudgetCategoryLimitReadModel>> GetCategoryLimitsAsync(string budgetId, CancellationToken ct = default)
    {
        return await db.BudgetCategoryLimits
            .Where(cl => cl.BudgetId == budgetId)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<SpendingSummaryReadModel>> GetSpendingSummaryAsync(string userId, int month, int year, CancellationToken ct = default)
    {
        return await db.SpendingSummaries
            .Where(s => s.UserId == userId && s.Month == month && s.Year == year)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<MonthlyStatsReadModel>> GetMonthlyStatsAsync(string userId, int year, CancellationToken ct = default)
    {
        return await db.MonthlyStats
            .Where(s => s.UserId == userId && s.Year == year)
            .OrderBy(s => s.Month)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
