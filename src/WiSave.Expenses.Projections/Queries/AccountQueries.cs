using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections.Queries;

public sealed class AccountQueries(ProjectionsDbContext db)
{
    public async Task<List<AccountReadModel>> GetAllAsync(string userId, CancellationToken ct = default)
    {
        return await db.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderBy(a => a.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<AccountReadModel?> GetByIdAsync(string accountId, string userId, CancellationToken ct = default)
    {
        return await db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, ct);
    }
}
