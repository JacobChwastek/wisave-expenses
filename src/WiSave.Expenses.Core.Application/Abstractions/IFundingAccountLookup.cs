using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.Core.Application.Abstractions;

public interface IFundingAccountLookup
{
    Task<FundingAccountCandidate?> GetAsync(string fundingAccountId, CancellationToken ct = default);
}

public sealed record FundingAccountCandidate(
    string FundingAccountId,
    string UserId,
    Currency Currency,
    bool IsActive);
