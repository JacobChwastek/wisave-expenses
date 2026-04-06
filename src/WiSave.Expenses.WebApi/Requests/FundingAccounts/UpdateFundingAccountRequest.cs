using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.WebApi.Requests.FundingAccounts;

public sealed record UpdateFundingAccountRequest(
    string Name,
    FundingAccountKind Kind,
    Currency Currency,
    string? Color = null);

public static class UpdateFundingAccountRequestExtensions
{
    public static UpdateFundingAccount ToCommand(
        this UpdateFundingAccountRequest request,
        Guid correlationId,
        string userId,
        string fundingAccountId)
        => new(
            correlationId,
            userId,
            fundingAccountId,
            request.Name,
            request.Kind,
            request.Currency,
            request.Color);
}
