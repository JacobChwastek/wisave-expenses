using WiSave.Expenses.Core.Application.Abstractions;
using WiSave.Expenses.Contracts.Models;
using WiSave.Expenses.Core.Domain.Funding;

namespace WiSave.Expenses.Core.Infrastructure.EventStore;

public sealed class FundingAccountLookup(
    IAggregateRepository<FundingAccount, FundingAccountId> repository) : IFundingAccountLookup
{
    public async Task<FundingAccountCandidate?> GetAsync(string fundingAccountId, CancellationToken ct = default)
    {
        var account = await repository.LoadAsync(new FundingAccountId(fundingAccountId), ct);
        return account is null
            ? null
            : new FundingAccountCandidate(account.Id.Value, account.UserId.Value, account.Currency, account.IsActive);
    }
}
