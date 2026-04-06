using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.WebApi.Requests.FundingAccounts;

public sealed record OpenFundingAccountRequest(
    string Name,
    FundingAccountKind Kind,
    Currency Currency,
    decimal OpeningBalance,
    string? Color = null);

public static class OpenFundingAccountRequestExtensions
{
    public static OpenFundingAccount ToCommand(this OpenFundingAccountRequest request, Guid correlationId, Guid userId)
        => new(
            correlationId,
            userId,
            request.Name,
            request.Kind,
            request.Currency,
            request.OpeningBalance,
            request.Color);
}
