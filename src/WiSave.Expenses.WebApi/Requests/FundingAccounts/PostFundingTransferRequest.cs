using WiSave.Expenses.Contracts.Commands.FundingAccounts;

namespace WiSave.Expenses.WebApi.Requests.FundingAccounts;

public sealed record PostFundingTransferRequest(
    decimal Amount,
    DateTimeOffset? PostedAtUtc = null);

public static class PostFundingTransferRequestExtensions
{
    public static PostFundingTransfer ToCommand(
        this PostFundingTransferRequest request,
        Guid correlationId,
        string userId,
        string fundingAccountId,
        string transferId)
        => new(
            correlationId,
            userId,
            fundingAccountId,
            transferId,
            request.Amount,
            request.PostedAtUtc ?? DateTimeOffset.UtcNow);
}
