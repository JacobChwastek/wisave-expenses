using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Core.Application.Abstractions;

namespace WiSave.Expenses.Core.Infrastructure.Postgres;

public sealed class PostgresCategoryRepository(ExpensesDbContext db) : ICategoryRepository
{
    public async Task<bool> ExistsAsync(string categoryId, string userId, CancellationToken ct = default)
    {
        return await db.Categories.AnyAsync(c => c.Id == categoryId && c.UserId == userId, ct);
    }

    public async Task<bool> SubcategoryExistsAsync(string subcategoryId, string categoryId, CancellationToken ct = default)
    {
        return await db.Subcategories.AnyAsync(s => s.Id == subcategoryId && s.CategoryId == categoryId, ct);
    }
}
