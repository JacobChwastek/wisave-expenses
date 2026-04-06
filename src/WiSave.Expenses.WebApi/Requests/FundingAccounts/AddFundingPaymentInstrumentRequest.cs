using WiSave.Expenses.Contracts.Commands.FundingAccounts;
using WiSave.Expenses.Contracts.Models;

namespace WiSave.Expenses.WebApi.Requests.FundingAccounts;

public sealed record AddFundingPaymentInstrumentRequest(
    string Name,
    PaymentInstrumentKind Kind,
    string? LastFourDigits = null,
    string? Network = null,
    string? Color = null);

public static class AddFundingPaymentInstrumentRequestExtensions
{
    public static AddFundingPaymentInstrument ToCommand(
        this AddFundingPaymentInstrumentRequest request,
        Guid correlationId,
        string userId,
        string fundingAccountId)
        => new(
            correlationId,
            userId,
            fundingAccountId,
            request.Name,
            request.Kind,
            request.LastFourDigits,
            request.Network,
            request.Color);
}
