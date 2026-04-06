using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections.Repositories;

public sealed class CreditCardAccountReadRepository(ProjectionsDbContext db)
{
    public async Task<List<CreditCardAccountReadModel>> GetAllAsync(string userId, CancellationToken ct = default)
    {
        return await db.CreditCardAccounts
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderBy(a => a.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<CreditCardAccountReadModel?> GetByIdAsync(string creditCardAccountId, string userId, CancellationToken ct = default)
    {
        return await db.CreditCardAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == creditCardAccountId && a.UserId == userId, ct);
    }

    public async Task<List<CreditCardStatementReadModel>> GetStatementsAsync(string creditCardAccountId, string userId, CancellationToken ct = default)
    {
        var ownsCard = await db.CreditCardAccounts
            .AsNoTracking()
            .AnyAsync(a => a.Id == creditCardAccountId && a.UserId == userId, ct);

        if (!ownsCard)
            return [];

        return await db.CreditCardStatements
            .Where(s => s.CreditCardAccountId == creditCardAccountId)
            .OrderByDescending(s => s.StatementDate)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
