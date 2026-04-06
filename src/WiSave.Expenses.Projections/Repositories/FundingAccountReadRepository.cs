using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections.Repositories;

public sealed class FundingAccountReadRepository(ProjectionsDbContext db)
{
    public async Task<List<FundingAccountReadModel>> GetAllAsync(string userId, CancellationToken ct = default)
    {
        return await db.FundingAccounts
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderBy(a => a.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<FundingAccountReadModel?> GetByIdAsync(string fundingAccountId, string userId, CancellationToken ct = default)
    {
        return await db.FundingAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == fundingAccountId && a.UserId == userId, ct);
    }

    public async Task<List<FundingPaymentInstrumentReadModel>> GetPaymentInstrumentsAsync(
        string fundingAccountId,
        string userId,
        CancellationToken ct = default)
    {
        return await db.FundingPaymentInstruments
            .Where(x => x.FundingAccountId == fundingAccountId && x.UserId == userId && x.IsActive)
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
